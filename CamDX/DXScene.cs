using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using System;
using System.Collections.Generic;

namespace CamDX
{
    public class DXScene : IDisposable
    {
        protected DXSceneNode _rootNode;
        public DXSceneNode RootNode { get { return _rootNode; } }

        public Matrix WorldMatrix { get; protected set; }
        public Matrix TransformMatrix { get; protected set; }
        protected Buffer _transformBuffer;

        // Lights
        protected GlobalLightsData _globalLights;
        public GlobalLightsData GlobalLights { get; set; }
        protected Buffer _globalLightsBuffer;
        
        public Dictionary<DXShader, RenderGroup> RenderGroups { get; protected set; }
        public DXCamera CurrentCamera { get; set; }

        public DXScene(SharpDX.Direct3D11.Device device)
        {
            RenderGroups = new Dictionary<DXShader, RenderGroup>();

            _transformBuffer = new Buffer(device, new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf<Matrix>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });

            _globalLightsBuffer = new Buffer(device, new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf<GlobalLightsData>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });

            WorldMatrix = Matrix.Identity;
            _rootNode = new DXSceneNode()
            {
                Position = new Vector3(),
                Orientation = Quaternion.Identity,
                Scale = new Vector3(1.0f)
            };
            _rootNode.UpdateTransfromMatrix();

            _globalLights = new GlobalLightsData()
            {
                Ambient = new Color4(0.5f, 0.5f, 0.5f, 1.0f),
                Direction = new Vector3(0.0f, -1.0f, 0.0f),
                Directional = new Color4(0.5f, 0.5f, 0.5f, 1.0f)
            };
        }

        //public void UpdateScene()
        //{
        //    // Prepares all models for render :
        //    // for each node:
        //    // - check if any model should be / not be rendered (rather check scene node AABB with camera frustum)
        //    // - check if any model have no render group assigned
        //    // - check if any model should have group changed
        //    // - update buffers for model
        //    // repeat for children

        //    // For now clear all render-groups, later may re-use dictinary and just invalidate entries
        //    RenderGroups.Clear();
        //    PrepareNodeForRender(_rootNode);
        //}

        //protected void PrepareNodeForRender(DXSceneNode node)
        //{
        //    // 1) node.GlobalAABBDerived; check if AABB is within camera frustum
        //    //      If is within:
        //    // 2) Prepare each attached model (do not check culling -> it will be per scene node)
        //    foreach(var model in node.AttachedObjects)
        //    {
        //        PrepareModelForRender(model);
        //    }
        //    // 3) Do same for all children
        //    foreach(var child in node.Children)
        //    {
        //        PrepareNodeForRender(child);
        //    }
        //}

        //protected void PrepareModelForRender(IModel model)
        //{
        //    // Check if model contains any sub-models
        //    if(model.SubModels != null)
        //    {
        //        // We have compund model -> just prepare each sub-model
        //        foreach(var submodel in model.SubModels)
        //        {
        //            PrepareModelForRender(submodel);
        //        }
        //    }
        //    else
        //    {
        //        // We have final render-model
        //        DXShader shader = model.Shader;
        //        if(shader != null)
        //        {
        //            // Add to render group with this shader
        //            RenderGroup rgroup;
        //            bool rgroupCreated = RenderGroups.TryGetValue(shader, out rgroup);
        //            if(!rgroupCreated)
        //            {
        //                rgroup = new RenderGroup();
        //                rgroup.Shader = shader;
        //                RenderGroups.Add(shader, rgroup);
        //            }
        //            rgroup.Models.Add(model);
        //        }
        //    }
        //}

        public void Render(DeviceContext device)
        {
            CurrentCamera.UpdateViewMatrix();
            CurrentCamera.UpdateProjectionMatrix();
            TransformMatrix = WorldMatrix * CurrentCamera.ViewMat * CurrentCamera.ProjMat;
            // finalTransform.Transpose();

            DataStream stream;
            var dataBox = device.MapSubresource(_globalLightsBuffer, 0, 
                MapMode.WriteDiscard, MapFlags.None);
            stream = new DataStream(dataBox.DataPointer, _globalLightsBuffer.Description.SizeInBytes, true, true);
            stream.Write(_globalLights);
            device.UnmapSubresource(_globalLightsBuffer, 0); //to update the data on GPU
            stream.Dispose();

            // device.VertexShader.SetConstantBuffer((int)ConstantBufferSlots.WorldViewProjMatrix, _transformBuffer);
            device.PixelShader.SetConstantBuffer((int)ConstantBufferSlots.GlobalLights, _globalLightsBuffer);

            RenderNode(device, _rootNode, TransformMatrix);

            //UpdateScene();
            //foreach (var rgroup in RenderGroups)
            //{
            //    rgroup.Value.Render(device, this);
            //}
        }

        void RenderNode(DeviceContext device, DXSceneNode node, Matrix derivedTrnasform)
        {
            Matrix transform = node.TransformationMatrix * derivedTrnasform;

            if(node.AttachedObjects.Count > 0)
            {
                transform.Transpose();

                DataStream stream;
                var dataBox = device.MapSubresource(_transformBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
                stream = new DataStream(dataBox.DataPointer, _transformBuffer.Description.SizeInBytes, true, true);
                stream.Write(transform);
                device.UnmapSubresource(_transformBuffer, 0); //to update the data on GPU
                stream.Dispose();
                
                transform.Transpose();

                device.VertexShader.SetConstantBuffer((int)ConstantBufferSlots.WorldViewProjMatrix, _transformBuffer);

                foreach(var model in node.AttachedObjects)
                {
                    model.Shader.RenderFirstPass(device);
                    model.Render(device);

                    for(int i = 1; i < model.Shader.Passes.Count; ++i)
                    {
                        model.Shader.RenderPass(device, i);
                        model.Render(device);
                    }
                }
            }

            foreach(var childNode in node.Children)
            {
                RenderNode(device, childNode, transform);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            this.SetField(ref _transformBuffer, null);
            this.SetField(ref _globalLightsBuffer, null);
        }
    }
}
