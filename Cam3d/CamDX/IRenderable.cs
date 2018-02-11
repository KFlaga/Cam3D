using SharpDX.Direct3D11;

namespace CamDX
{
    public interface IRenderable
    {
        void Render(DeviceContext deviceContext);
    }
}
