using TagTool.Cache;
using TagTool.Common;
using TagTool.IO;
using TagTool.Serialization;
using TagTool.Tags.Definitions;
using TagTool.Tags.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using TagTool.Tags;
using System.Linq;

namespace TagTool.Geometry
{
    public class RenderGeometryConverter
    {
        private HaloOnlineCacheContext CacheContext { get; }
        private CacheFile BlamCache;

        public RenderGeometryConverter(HaloOnlineCacheContext cacheContext, CacheFile blamCache)
        {
            CacheContext = cacheContext;
            BlamCache = blamCache;
        }

        private static void ConvertVertices<T>(int count, Func<T> readVertex, Action<T, int> writeVertex)
        {
            for (var i = 0; i < count; i++)
                writeVertex(readVertex(), i);
        }

        public void ConvertVertexBuffer(RenderGeometryApiResourceDefinition resourceDefinition, Stream inputStream, Stream outputStream, int vertexBufferIndex, int previousVertexBufferCount)
        {
            var vertexBuffer = resourceDefinition.VertexBuffers[vertexBufferIndex].Definition;

            var count = vertexBuffer.Count;

            var startPos = (int)outputStream.Position;
            vertexBuffer.Data.Address = new CacheAddress(CacheAddressType.Resource, startPos);

            var inVertexStream = VertexStreamFactory.Create(BlamCache.Version, inputStream);
            var outVertexStream = VertexStreamFactory.Create(CacheContext.Version, outputStream);

            switch (vertexBuffer.Format)
            {
                case VertexBufferFormat.World:
                    ConvertVertices(count, inVertexStream.ReadWorldVertex, (v, i) =>
                    {
                        v.Tangent = new RealQuaternion(-Math.Abs(v.Tangent.I), -Math.Abs(v.Tangent.J), Math.Abs(v.Tangent.K), Math.Abs(v.Tangent.W)); // great results for H3 armors
                        outVertexStream.WriteWorldVertex(v);
                    });
                    break;

                case VertexBufferFormat.Rigid:
                    ConvertVertices(count, inVertexStream.ReadRigidVertex, (v, i) =>
                    {
                        v.Tangent = new RealQuaternion(-Math.Abs(v.Tangent.I), -Math.Abs(v.Tangent.J), Math.Abs(v.Tangent.K), Math.Abs(v.Tangent.W)); // great results for H3 armors
                        outVertexStream.WriteRigidVertex(v);
                    });
                    break;

                case VertexBufferFormat.Skinned:
                    ConvertVertices(count, inVertexStream.ReadSkinnedVertex, (v, i) =>
                    {
                        v.Tangent = new RealQuaternion(-Math.Abs(v.Tangent.I), -Math.Abs(v.Tangent.J), Math.Abs(v.Tangent.K), Math.Abs(v.Tangent.W)); // great results for H3 armors
                        outVertexStream.WriteSkinnedVertex(v);
                    });
                    break;

                case VertexBufferFormat.StaticPerPixel:
                    ConvertVertices(count, inVertexStream.ReadStaticPerPixelData, (v, i) => outVertexStream.WriteStaticPerPixelData(v));
                    break;

                case VertexBufferFormat.StaticPerVertex:
                    ConvertVertices(count, inVertexStream.ReadStaticPerVertexData, (v, i) =>
                    {
                        v.Texcoord1 = ConvertNormal(v.Texcoord1);
                        v.Texcoord2 = ConvertNormal(v.Texcoord2);
                        v.Texcoord3 = ConvertNormal(v.Texcoord3);
                        v.Texcoord4 = ConvertNormal(v.Texcoord4);
                        v.Texcoord5 = ConvertNormal(v.Texcoord5);
                        outVertexStream.WriteStaticPerVertexData(v);
                    });
                    break;

                case VertexBufferFormat.AmbientPrt:
                    ConvertVertices(vertexBuffer.Count = previousVertexBufferCount, inVertexStream.ReadAmbientPrtData, (v, i) => outVertexStream.WriteAmbientPrtData(v));
                    break;

                case VertexBufferFormat.LinearPrt:
                    ConvertVertices(count, inVertexStream.ReadLinearPrtData, (v, i) =>
                    {
                        v.BlendWeight = ConvertNormal(v.BlendWeight);
                        outVertexStream.WriteLinearPrtData(v);
                    });
                    break;

                case VertexBufferFormat.QuadraticPrt:
                    ConvertVertices(count, inVertexStream.ReadQuadraticPrtData, (v, i) => outVertexStream.WriteQuadraticPrtData(v));
                    break;

                case VertexBufferFormat.StaticPerVertexColor:
                    ConvertVertices(count, inVertexStream.ReadStaticPerVertexColorData, (v, i) => outVertexStream.WriteStaticPerVertexColorData(v));
                    break;

                case VertexBufferFormat.Decorator:
                    ConvertVertices(count, inVertexStream.ReadDecoratorVertex, (v, i) => outVertexStream.WriteDecoratorVertex(v));
                    break;

                case VertexBufferFormat.World2:
                    vertexBuffer.Format = VertexBufferFormat.World;
                    goto case VertexBufferFormat.World;

                case VertexBufferFormat.Unknown1A:
                    ConvertVertices(count, inVertexStream.ReadUnknown1A, (v, i) => outVertexStream.WriteUnknown1A(v));
                    break;

                case VertexBufferFormat.Unknown1B:
                    ConvertVertices(count, inVertexStream.ReadUnknown1B, (v, i) => outVertexStream.WriteUnknown1B(v));
                    break;

                case VertexBufferFormat.ParticleModel:
                    ConvertVertices(count, inVertexStream.ReadParticleModelVertex, (v, i) => outVertexStream.WriteParticleModelVertex(v));
                    break;

                case VertexBufferFormat.TinyPosition:
                    ConvertVertices(count, inVertexStream.ReadTinyPositionVertex, (v, i) => 
                    {
                        v.Position = ConvertPositionShort(v.Position);
                        v.Variant = (ushort) ((v.Variant >> 8) & 0xFF);
                        v.Normal = ConvertNormal(v.Normal);
                        outVertexStream.WriteTinyPositionVertex(v);
                    });
                    break;

                default:
                    throw new NotSupportedException(vertexBuffer.Format.ToString());
            }

            vertexBuffer.Data.Size = (int)outputStream.Position - startPos;
            vertexBuffer.VertexSize = (short)(vertexBuffer.Data.Size / vertexBuffer.Count);
        }

        public void ConvertIndexBuffer(RenderGeometryApiResourceDefinition resourceDefinition, Stream inputStream, Stream outputStream, int indexBufferIndex)
        {
            var indexBuffer = resourceDefinition.IndexBuffers[indexBufferIndex].Definition;

            var indexCount = indexBuffer.Data.Size / 2;

            var inIndexStream = new IndexBufferStream(
                inputStream, 
                EndianFormat.BigEndian);

            var outIndexStream = new IndexBufferStream(
                outputStream,
                EndianFormat.LittleEndian);

            StreamUtil.Align(outputStream, 4);
            indexBuffer.Data.Address = new CacheAddress(CacheAddressType.Resource, (int)outputStream.Position);

            for (var j = 0; j < indexCount; j++)
                outIndexStream.WriteIndex(inIndexStream.ReadIndex());
        }

        public ushort CreateIndexBuffer(RenderGeometryApiResourceDefinition resourceDefinition, Stream outputStream, int count)
        {
            resourceDefinition.IndexBuffers.Add(new D3DPointer<IndexBufferDefinition>
            {
                Definition = new IndexBufferDefinition
                {
                    Format = IndexBufferFormat.TriangleStrip,
                    Data = new TagData
                    {
                        Size = count * 2,
                        Unused4 = 0,
                        Unused8 = 0,
                        Address = new CacheAddress(),
                        Unused10 = 0
                    }
                }
            });

            var indexBuffer = resourceDefinition.IndexBuffers.Last().Definition;

            var indexCount = indexBuffer.Data.Size / 2;
            
            var outIndexStream = new EndianWriter(
                outputStream,
                EndianFormat.LittleEndian);

            StreamUtil.Align(outputStream, 4);
            indexBuffer.Data.Address = new CacheAddress(CacheAddressType.Resource, (int)outputStream.Position);

            for (var j = 0; j < indexCount; j++)
                outIndexStream.Write((short)j);

            return (ushort)resourceDefinition.IndexBuffers.IndexOf(resourceDefinition.IndexBuffers.Last());
        }

        public RenderGeometry Convert(Stream cacheStream, RenderGeometry geometry, Dictionary<ResourceLocation, Stream> resourceStreams)
        {
            //
            // Convert byte[] of UnknownBlock
            //

            foreach(var block in geometry.Unknown2)
            {
                var data = block.Unknown3;
                if(data != null || data.Length != 0)
                {
                    var result = new byte[data.Length];

                    using (var inputReader = new EndianReader(new MemoryStream(data), EndianFormat.BigEndian))
                    using (var outputWriter = new EndianWriter(new MemoryStream(result), EndianFormat.LittleEndian))
                    {
                        while (!inputReader.EOF)
                            outputWriter.Write(inputReader.ReadUInt32());

                        block.Unknown3 = result;
                    }
                }
            }

            //
            // Convert UnknownSection.Unknown byte[] endian
            //

            for (int i = 0; i < geometry.UnknownSections.Count; i++)
            {
                byte[] dataref = geometry.UnknownSections[i].Unknown;

                if (dataref.Length == 0)
                    continue;

                using (var outStream = new MemoryStream())
                using (var outReader = new BinaryReader(outStream))
                using (var outWriter = new EndianWriter(outStream, EndianFormat.LittleEndian))
                using (var stream = new MemoryStream(dataref))
                using (var reader = new EndianReader(stream, EndianFormat.BigEndian))
                {
                    var dataContext = new DataSerializationContext(reader, outWriter);
                    var header = CacheContext.Deserializer.Deserialize<ScenarioLightmapBspDataSection.Header>(dataContext);

                    var section = new ScenarioLightmapBspDataSection
                    {
                        Headers = new List<ScenarioLightmapBspDataSection.Header>
                        {
                            header
                        },
                        VertexLists = new ScenarioLightmapBspDataSection.VertexList
                        {
                            Vertex = new List<ScenarioLightmapBspDataSection.VertexList.Datum>()
                        }
                    };

                    CacheContext.Serializer.Serialize(dataContext, header);

                    while (reader.BaseStream.Position < dataref.Length) // read the rest of dataref
                    {
                        if (section.Headers.Count == 2) // remove "wrongfully" added ones
                            section.Headers.RemoveAt(1);

                        section.Headers.Add(CacheContext.Deserializer.Deserialize<ScenarioLightmapBspDataSection.Header>(dataContext));

                        // if some values match header1, continue
                        if (section.Headers[0].Position == section.Headers[1].Position)
                        {
                            header = section.Headers[1];
                            CacheContext.Serializer.Serialize(dataContext, header);

                            while (reader.BaseStream.Position < dataref.Length)
                            {
                                section.VertexLists.Vertex.Add(new ScenarioLightmapBspDataSection.VertexList.Datum { Value = reader.ReadByte() });
                                outWriter.Write(section.VertexLists.Vertex[section.VertexLists.Vertex.Count - 1].Value);
                            }
                        }
                        else // if read data doesn't match, go back and just read 4 bytes
                        {
                            reader.BaseStream.Position = reader.BaseStream.Position - 0x2C; // if read data doesn't match, go back and serialize 

                            section.VertexLists.Vertex.Add(new ScenarioLightmapBspDataSection.VertexList.Datum { Value = reader.ReadByte() });
                            outWriter.Write(section.VertexLists.Vertex[section.VertexLists.Vertex.Count - 1].Value);

                            section.VertexLists.Vertex.Add(new ScenarioLightmapBspDataSection.VertexList.Datum { Value = reader.ReadByte() });
                            outWriter.Write(section.VertexLists.Vertex[section.VertexLists.Vertex.Count - 1].Value);

                            section.VertexLists.Vertex.Add(new ScenarioLightmapBspDataSection.VertexList.Datum { Value = reader.ReadByte() });
                            outWriter.Write(section.VertexLists.Vertex[section.VertexLists.Vertex.Count - 1].Value);

                            section.VertexLists.Vertex.Add(new ScenarioLightmapBspDataSection.VertexList.Datum { Value = reader.ReadByte() });
                            outWriter.Write(section.VertexLists.Vertex[section.VertexLists.Vertex.Count - 1].Value);
                        }
                    }

                    // Write back to tag
                    outStream.Position = 0;
                    geometry.UnknownSections[i].Unknown = outStream.ToArray();
                }
            }

            //
            // Set up ElDorado resource reference
            //

            geometry.Resource = new PageableResource
            {
                Page = new RawPage
                {
                    Index = -1,
                },
                Resource = new TagResource
                {
                    Type = TagResourceType.RenderGeometry,
                    DefinitionData = new byte[0x30],
                    DefinitionAddress = new CacheAddress(CacheAddressType.Definition, 0),
                    ResourceFixups = new List<TagResource.ResourceFixup>(),
                    ResourceDefinitionFixups = new List<TagResource.ResourceDefinitionFixup>(),
                }
            };

            //
            // Load Blam resource data
            //

            var rsrcData = BlamCache.GetRawFromID(geometry.ZoneAssetHandle);

            if (rsrcData == null)
                return geometry;

            //
            // Load Blam resource definition
            //

            var rsrcDefEntry = BlamCache.ResourceGestalt.TagResources[geometry.ZoneAssetHandle & ushort.MaxValue];

            var rsrcDef = new RenderGeometryApiResourceDefinition
            {
                VertexBuffers = new List<D3DPointer<VertexBufferDefinition>>(),
                IndexBuffers = new List<D3DPointer<IndexBufferDefinition>>()
            };

            using (var rsrcDefStream = new MemoryStream(BlamCache.ResourceGestalt.FixupInformation))
            using (var rsrcDefReader = new EndianReader(rsrcDefStream, EndianFormat.BigEndian))
            {
                rsrcDefReader.SeekTo(rsrcDefEntry.FixupInformationOffset + (rsrcDefEntry.FixupInformationLength - 24));

                var vertexBufferCount = rsrcDefReader.ReadInt32();
                rsrcDefReader.Skip(8);
                var indexBufferCount = rsrcDefReader.ReadInt32();

                rsrcDefReader.SeekTo(rsrcDefEntry.FixupInformationOffset);

                for (var i = 0; i < vertexBufferCount; i++)
                {
                    rsrcDef.VertexBuffers.Add(new D3DPointer<VertexBufferDefinition>
                    {
                        Definition = new VertexBufferDefinition
                        {
                            Count = rsrcDefReader.ReadInt32(),
                            Format = (VertexBufferFormat)rsrcDefReader.ReadInt16(),
                            VertexSize = rsrcDefReader.ReadInt16(),
                            Data = new TagData
                            {
                                Size = rsrcDefReader.ReadInt32(),
                                Unused4 = rsrcDefReader.ReadInt32(),
                                Unused8 = rsrcDefReader.ReadInt32(),
                                Address = new CacheAddress(CacheAddressType.Memory, rsrcDefReader.ReadInt32()),
                                Unused10 = rsrcDefReader.ReadInt32()
                            }
                        }
                    });
                }

                rsrcDefReader.Skip(vertexBufferCount * 12);

                for (var i = 0; i < indexBufferCount; i++)
                {
                    rsrcDef.IndexBuffers.Add(new D3DPointer<IndexBufferDefinition>
                    {
                        Definition = new IndexBufferDefinition
                        {
                            Format = (IndexBufferFormat)rsrcDefReader.ReadInt32(),
                            Data = new TagData
                            {
                                Size = rsrcDefReader.ReadInt32(),
                                Unused4 = rsrcDefReader.ReadInt32(),
                                Unused8 = rsrcDefReader.ReadInt32(),
                                Address = new CacheAddress(CacheAddressType.Memory, rsrcDefReader.ReadInt32()),
                                Unused10 = rsrcDefReader.ReadInt32()
                            }
                        }
                    });
                }
            }

            //
            // Convert Blam data to ElDorado data
            //

            using (var dataStream = new MemoryStream())
            using (var blamResourceStream = new MemoryStream(rsrcData))
            {
                //
                // Convert Blam render_geometry_api_resource_definition
                //

                for (int i = 0, prevVertCount = -1; i < rsrcDef.VertexBuffers.Count; i++, prevVertCount = rsrcDef.VertexBuffers[i - 1].Definition.Count)
                {
                    blamResourceStream.Position = rsrcDefEntry.ResourceFixups[i].Offset;
                    ConvertVertexBuffer(rsrcDef, blamResourceStream, dataStream, i, prevVertCount);
                }

                for (var i = 0; i < rsrcDef.IndexBuffers.Count; i++)
                {
                    blamResourceStream.Position = rsrcDefEntry.ResourceFixups[rsrcDef.VertexBuffers.Count * 2 + i].Offset;
                    ConvertIndexBuffer(rsrcDef, blamResourceStream, dataStream, i);
                }

                foreach (var mesh in geometry.Meshes)
                {
                    if (!mesh.Flags.HasFlag(MeshFlags.MeshIsUnindexed))
                        continue;

                    var indexCount = 0;

                    foreach (var part in mesh.Parts)
                        indexCount += part.IndexCount;

                    mesh.IndexBufferIndices[0] = CreateIndexBuffer(rsrcDef, dataStream, indexCount);
                }
                
                //
                // Finalize the new ElDorado geometry resource
                //

                geometry.Resource = new PageableResource
                {
                    Page = new RawPage
                    {
                        Index = -1
                    },
                    Resource = new TagResource
                    {
                        Type = TagResourceType.RenderGeometry,
                        ResourceFixups = new List<TagResource.ResourceFixup>(),
                        ResourceDefinitionFixups = new List<TagResource.ResourceDefinitionFixup>(),
                        Unknown2 = 1
                    }
                };

                dataStream.Position = 0;

                var resourceContext = new ResourceSerializationContext(geometry.Resource);
                CacheContext.Serializer.Serialize(resourceContext, rsrcDef);
                geometry.Resource.ChangeLocation(ResourceLocation.Resources);
                var resource = geometry.Resource;

                if (resource == null)
                    throw new ArgumentNullException("resource");

                if (!dataStream.CanRead)
                    throw new ArgumentException("The input stream is not open for reading", "dataStream");

                var cache = CacheContext.GetResourceCache(ResourceLocation.Resources);

                if (!resourceStreams.ContainsKey(ResourceLocation.Resources))
                {
                    resourceStreams[ResourceLocation.Resources] = new MemoryStream();

                    using (var resourceStream = CacheContext.OpenResourceCacheRead(ResourceLocation.Resources))
                        resourceStream.CopyTo(resourceStreams[ResourceLocation.Resources]);
                }

                var dataSize = (int)(dataStream.Length - dataStream.Position);
                var data = new byte[dataSize];
                dataStream.Read(data, 0, dataSize);

                resource.Page.Index = cache.Add(resourceStreams[ResourceLocation.Resources], data, out uint compressedSize);
                resource.Page.CompressedBlockSize = compressedSize;
                resource.Page.UncompressedBlockSize = (uint)dataSize;
                resource.DisableChecksum();
            }

            return geometry;
        }

        /// <summary> 
        /// Change basis [0,1] to [-1,1] uniformly
        /// </summary>
        private static float ConvertFromNormalBasis(float value)
        {
            value = VertexElementStream.Clamp(value, 0.0f, 1.0f);
            return 2.0f * (value - 0.5f);
        }

        /// <summary> 
        /// Change basis [-1,1] to [0,1] uniformly
        /// </summary>
        private static float ConvertToNormalBasis(float value)
        {
            value = VertexElementStream.Clamp(value);
            return (value / 2.0f) + 0.5f;
        }

        /// <summary>
        /// Modify value to account for some rounding error
        /// </summary>
        private static float FixRoundingShort(float value)
        {
            value = VertexElementStream.Clamp(value);
            if (value > 0 && value < 1)
            {
                value += 1.0f / 32767.0f;
                value = VertexElementStream.Clamp(value);
            }
            return value;
        }

        /// <summary>
        /// Convert H3 normal to HO normal for tinyposition vertex
        /// </summary>
        public RealQuaternion ConvertNormal(RealQuaternion normal)
        {
            return new RealQuaternion(normal.ToArray().Select(e => ConvertToNormalBasis(e)).Reverse());
        }

        /// <summary>
        /// Convert H3 position to HO position including rounding error for tinyposition vertex
        /// </summary>
        public RealVector3d ConvertPositionShort(RealVector3d position)
        {
            return new RealVector3d(position.ToArray().Select(e => FixRoundingShort(ConvertFromNormalBasis(e))).ToArray());
        }
    }
}