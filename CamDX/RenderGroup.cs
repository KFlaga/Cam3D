using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Render(DeviceContext device)
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
