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
    public abstract class AlgorithmParameter
    {
        public string Name { get; protected set; }
        public string ShortName { get; protected set; }
        public string TypeName { get; protected set; }
        public abstract object ActualValue { get; set; }
        public abstract IParameterInput Input { get; set; }

        protected AlgorithmParameter(string name, string sname, string typename)
        {
            Name = name;
            ShortName = sname;
            TypeName = typename;
        }

        public static AlgorithmParameter FindParameter(string name, List<AlgorithmParameter> paramsList)
        {
            for(int i = 0; i < paramsList.Count; i++)
            {
                if(string.Compare(paramsList[i].ShortName, name) == 0)
                {
                    return paramsList[i];
                }
            }
            return null;
        }

        public static object FindValue(string name, List<AlgorithmParameter> paramsList)
        {
            for(int i = 0; i < paramsList.Count; i++)
            {
                if(string.Compare(paramsList[i].ShortName, name) == 0)
                {
                    return paramsList[i].ActualValue;
                }
            }
            return null;
        }

        public static T FindValue<T>(string name, List<AlgorithmParameter> paramsList)
        {
            for(int i = 0; i < paramsList.Count; i++)
            {
                if(string.Compare(paramsList[i].ShortName, name) == 0)
                {
                    return (T)paramsList[i].ActualValue;
                }
            }
            return default(T);
        }

        public abstract void ReadFromXml(XmlNode node);

        // Expects 'node' to constain childs with parameters with attributes 'id' and 'value'
        public static void ReadParametersFromXml(List<AlgorithmParameter> parameters, XmlNode node)
        {
            //<Parameters>
            //  <Parameter id="aaa" value="3"/>
            //</Parameters>
            
            foreach(XmlNode paramNode in node.ChildNodes)
            {
                if(paramNode.Attributes != null && 
                    paramNode.Attributes.GetNamedItem("id") != null)
                {
                    string id = paramNode.Attributes["id"].Value;
                    AlgorithmParameter param = parameters.Find((p) => { return p.ShortName == id; });
#if DEBUG
                    param.ReadFromXml(paramNode);
#else
                    param?.ReadFromXml(paramNode);
#endif
                }
            }
        }
    }

    public abstract class AlgorithmParameter<T> : AlgorithmParameter
    {
        public override object ActualValue
        {
            get
            {
                return Value;
            }

            set
            {
                Value = (T)value;
            }
        }

        public T Value { get; set; }
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

        protected AlgorithmParameter(string name, string sname, string typename) : 
            base(name, sname, typename)
        {
            DefaultValue = default(T);
            MaxValue = default(T);
            MinValue = default(T);
            Value = default(T);
        }

        protected AlgorithmParameter(string name, string sname, string typename, 
            T defVal, T minVal, T maxVal) : 
            base(name, sname, typename)
        {
            DefaultValue = defVal;
            MaxValue = maxVal;
            MinValue = minVal;
            Value = defVal;
        }

        protected void ParameterValueChanged(object sender, ParameterValueChangedEventArgs e)
        {
            Value = (T)e.NewValue;
        }
    }

}