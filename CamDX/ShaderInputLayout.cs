using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using System.Xml;

namespace CamDX
{
    public enum LayoutElementType : int
    {
        None = 0,
        Position3 = 0x1,
        Position4 = 0x2,
        Color4 = 0x4,
        Normal3 = 0x8,
        Normal4 = 0x10,
        TexCoords2 = 0x20,
        TypeMask = 0x00FF,

        NoSlot = 0x100,
        Slot0 = 0x200,
        Slot1 = 0x400,
        Slot2 = 0x800,
        Slot3 = 0x1000,
        SlotMask = 0xFF00,
    }

    public static class InputLayoutCreator
    {
        public static LayoutElementType CreateElementType(LayoutElementType dataType, 
            LayoutElementType slot = LayoutElementType.NoSlot)
        {
            return dataType | slot;
        }

        public static InputLayout CreateInputLayout(Device dxDevice, ShaderSignature signature,
            LayoutElementType[] elementTypes)
        {
            InputElement[] elements = new InputElement[elementTypes.Length];

            int offset = 0;
            for(int i = 0; i < elements.Length; ++i)
            {
                var type = elementTypes[i];
                int size;
                var element = CreateInputElement(type, out size);
                element.AlignedByteOffset = offset;
                elements[i] = element;
                offset += size;
            }

            return new InputLayout(dxDevice, signature, elements);
        }

        public static InputElement CreateInputElement(LayoutElementType type, out int elemSize)
        {
            InputElement element = new InputElement();
            switch(type & LayoutElementType.TypeMask)
            {
                case LayoutElementType.Position3:
                    element.SemanticName = "POSITION";
                    element.Format = SharpDX.DXGI.Format.R32G32B32_Float;
                    elemSize = 12;
                    break;
                case LayoutElementType.Position4:
                    element.SemanticName = "POSITION";
                    element.Format = SharpDX.DXGI.Format.R32G32B32A32_Float;
                    elemSize = 16;
                    break;
                case LayoutElementType.Normal3:
                    element.SemanticName = "NORMAL";
                    element.Format = SharpDX.DXGI.Format.R32G32B32_Float;
                    elemSize = 12;
                    break;
                case LayoutElementType.Normal4:
                    element.SemanticName = "NORMAL";
                    element.Format = SharpDX.DXGI.Format.R32G32B32A32_Float;
                    elemSize = 16;
                    break;
                case LayoutElementType.Color4:
                    element.SemanticName = "COLOR";
                    element.Format = SharpDX.DXGI.Format.R32G32B32A32_Float;
                    elemSize = 16;
                    break;
                case LayoutElementType.TexCoords2:
                    element.SemanticName = "TEXCOORD";
                    element.Format = SharpDX.DXGI.Format.R32G32_Float;
                    elemSize = 8;
                    break;
                default:
                    element.SemanticName = "POSITION";
                    element.Format = SharpDX.DXGI.Format.R32G32B32_Float;
                    elemSize = 12;
                    break;
            }

            switch(type & LayoutElementType.SlotMask)
            {
                case LayoutElementType.Slot0:
                    element.SemanticName += "0";
                    element.Slot = 0;
                    break;
                case LayoutElementType.Slot1:
                    element.SemanticName += "1";
                    element.Slot = 1;
                    break;
                case LayoutElementType.Slot2:
                    element.SemanticName += "2";
                    element.Slot = 2;
                    break;
                case LayoutElementType.Slot3:
                    element.SemanticName += "3";
                    element.Slot = 3;
                    break;
                default:
                    element.Slot = 0;
                    break;
            }

            return element;
        }

        public static LayoutElementType ParseTypeFromXml(XmlNode typeNode)
        {
            // < ElementType type = "Position4" slot = "0" />
            string typeName = typeNode.Attributes["type"].Value;
            LayoutElementType elementType = LayoutElementType.Position3;
            if(typeName == "Position3")
                elementType = LayoutElementType.Position3;
            else if(typeName == "Position4")
                elementType = LayoutElementType.Position4;
            else if(typeName == "Normal4")
                elementType = LayoutElementType.Normal4;
            else if(typeName == "Normal3")
                elementType = LayoutElementType.Normal3;
            else if(typeName == "Color4")
                elementType = LayoutElementType.Color4;
            else if(typeName == "TexCoords2")
                elementType = LayoutElementType.TexCoords2;

            int slot;
            bool haveSlot = int.TryParse(typeNode.Attributes["slot"]?.Value, out slot);
            if(haveSlot)
            {
                elementType |= (LayoutElementType)((int)LayoutElementType.NoSlot << slot);
            }
            else
                elementType |= LayoutElementType.NoSlot;

            return elementType;
        }

        public static InputLayout CreateInputLayoutFromXml(Device dxDevice, ShaderSignature signature, XmlNode layoutNode)
        {
            InputElement[] elements = new InputElement[layoutNode.ChildNodes.Count];

            int offset = 0;
            XmlNode elemNode = layoutNode.FirstChild;
            for(int i = 0; i < elements.Length; ++i)
            {
                var type = ParseTypeFromXml(elemNode);
                int size;
                var element = CreateInputElement(type, out size);
                element.AlignedByteOffset = offset;
                elements[i] = element;
                offset += size;
                elemNode = elemNode.NextSibling;
            }

            return new InputLayout(dxDevice, signature, elements);
        }
    }
}
