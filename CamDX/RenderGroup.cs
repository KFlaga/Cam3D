using SharpDX.Direct3D11;
using System.Collections.Generic;

namespace CamDX
{
    // Group of models to render with same material
    public class RenderGroup
    {
        public List<IModel> Models { get; set; } = new List<IModel>();
        public DXShader Shader { get; set; }

        public RenderGroup()
        {

        }

        public void Render(DeviceContext device, DXScene scene)
        {
            Shader.RenderFirstPass(device);

            foreach(var model in Models)
            {
                model.Render(device);
            }

            for(int i = 1; i < Shader.Passes.Count; ++i)
            {
                Shader.RenderPass(device, i);

                foreach(var model in Models)
                {
                    model.Render(device);
                }
            }
        }
    }
}
