using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamDX
{
    // Group of models to render with same shaders / textures
    public class RenderGroup
    {
        public List<DX11Model> Models { get; set; }

        public VertexShader VertexShader { get; set; }
        public PixelShader PixelShader { get; set; }
        public InputLayout InputLayout { get; set; }

        //   public Texture2D Texture { get; set; }

        public RenderGroup()
        {
            Models = new List<DX11Model>();
        }

        public void Render(DeviceContext device)
        {
            device.VertexShader.Set(VertexShader);
            device.PixelShader.Set(PixelShader);
            device.InputAssembler.InputLayout = InputLayout;

            foreach (var model in Models)
            {
                model.Render(device);
            }
        }
    }
}
