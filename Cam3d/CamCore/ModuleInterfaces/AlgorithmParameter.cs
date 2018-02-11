using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace CamCore
{
    /*
        Set of parameters used by image processors (ie. detectors)
        Contains definitions of parameter and actual values
        Class used to pass info on necessary params outside processor (ie. for input)
        as well as to recieve values of them in universal way for every processor 
    */
    public abstract class IAlgorithmParameter
    {
        public abstract string Name { get; set; }
        public abstract string Id { get; set; }
        public abstract string TypeName { get; set; }
        public abstract object Value { get; set; }
        public abstract IParameterInput Input { get; set; }

        public abstract void ReadFromXml(XmlNode parameterNode);

        public static IAlgorithmParameter FindParameter(string name, List<IAlgorithmParameter> parameters)
        {
            for(int i = 0; i < parameters.Count; i++)
            {
                if(string.Compare(parameters[i].Id, name) == 0)
                {
                    return parameters[i];
                }
            }
            return null;
        }

        public static object FindValue(string name, List<IAlgorithmParameter> parameters)
        {
            for(int i = 0; i < parameters.Count; i++)
            {
                if(string.Compare(parameters[i].Id, name) == 0)
                {
                    return parameters[i].Value;
                }
            }
            return null;
        }

        public static T FindValue<T>(string name, List<IAlgorithmParameter> paramsList)
        {
            for(int i = 0; i < paramsList.Count; i++)
            {
                if(string.Compare(paramsList[i].Id, name) == 0)
                {
                    return (T)paramsList[i].Value;
                }
            }
            return default(T);
        }

        // public abstract void WriteToXml(XmlDocument xmlDoc, XmlNode node);

        // Expects 'node' to constain childs with parameters with attributes 'id' and 'value'
        public static void ReadParametersFromXml(List<IAlgorithmParameter> parameters, XmlNode node)
        {
            //<Parameters>
            //  <Parameter id="aaa" value="3"/>
            //</Parameters>
            
            foreach(XmlNode paramNode in node.ChildNodes)
            {
                if(paramNode.Attributes != null && paramNode.Attributes.GetNamedItem("id") != null)
                {
                    string id = paramNode.Attributes["id"].Value;
                    IAlgorithmParameter param = parameters.Find((p) => { return p.Id == id; });
#if DEBUG
                    param.ReadFromXml(paramNode);
#else
                    param?.ReadFromXml(paramNode);
#endif
                }
            }
        }

        public static XmlNode WriteParametersToXml(XmlDocument xmlDoc, List<IAlgorithmParameter> parameters)
        {
            //<Parameters>
            //  <Parameter id="aaa" value="3"/>
            //</Parameters>

            XmlNode parametersNode = xmlDoc.CreateElement("Parameters");
            foreach(var parameter in parameters)
            {
                XmlNode paramNode = xmlDoc.CreateElement("Parameter");
                XmlAttribute idAtt = xmlDoc.CreateAttribute("id");
                idAtt.Value = parameter.Id;
                paramNode.Attributes.Append(idAtt);

                // parameter.WriteToXml(xmlDoc, paramNode);
                throw new NotImplementedException();

                parametersNode.AppendChild(paramNode);
            }
            return parametersNode;
        }
    }

    public abstract class AlgorithmParameter<T> : IAlgorithmParameter
    {

        public override string Name { get; set; }
        public override string Id { get; set; }
        public override string TypeName { get; set; }

        public override object Value
        {
            get
            {
                return ActualValue;
            }

            set
            {
                ActualValue = (T)value;
            }
        }

        public T ActualValue { get; set; }
        public T MaxValue { get; set; }
        public T MinValue { get; set; }
        public T DefaultValue { get; set; }

        protected IParameterInput _input = null;
        public override IParameterInput Input
        {
            get { return _input; }
            set
            {
                if(_input != null)
                    _input.InputValueChanged -= ParameterValueChanged;
                _input = value;
                if(_input != null)
                    _input.InputValueChanged += ParameterValueChanged;
            }
        }

        protected AlgorithmParameter(string name, string id, string typename)
        {
            Name = name;
            Id = id;
            TypeName = typename;
            DefaultValue = default(T);
            MaxValue = default(T);
            MinValue = default(T);
            ActualValue = default(T);
        }

        protected AlgorithmParameter(string name, string id, string typename, T defVal, T minVal, T maxVal)
        {
            Name = name;
            Id = id;
            TypeName = typename;
            DefaultValue = defVal;
            MaxValue = maxVal;
            MinValue = minVal;
            ActualValue = defVal;
        }

        protected void ParameterValueChanged(object sender, ParameterValueChangedEventArgs e)
        {
            ActualValue = (T)e.NewValue;
        }
    }

}