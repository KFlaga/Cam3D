using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace CamCore
{
    public class IntParameter : AlgorithmParameter<int>
    {
        public IntParameter(string name, string sname) :
            base(name, sname, typeof(int).Name)
        { }

        public IntParameter(string name, string sname, 
            int defVal, int minVal, int maxVal) :
            base(name, sname, typeof(int).Name, defVal, 
                Math.Min(minVal, maxVal), Math.Max(minVal, maxVal))
        { }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="3"/>
            Value = int.Parse(node.Attributes["value"].Value);
        }
    }

    public class FloatParameter : AlgorithmParameter<float>
    {
        public FloatParameter(string name, string sname) :
            base(name, sname, typeof(float).Name)
        { }

        public FloatParameter(string name, string sname, 
            float defVal, float minVal, float maxVal) :
            base(name, sname, typeof(float).Name, defVal, 
                Math.Min(minVal, maxVal), Math.Max(minVal, maxVal))
        { }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="3"/>
            Value = float.Parse(node.Attributes["value"].Value);
        }
    }

    public class DoubleParameter : AlgorithmParameter<double>
    {
        public DoubleParameter(string name, string sname) :
            base(name, sname, typeof(double).Name)
        { }

        public DoubleParameter(string name, string sname, 
            double defVal, double minVal, double maxVal) :
            base(name, sname, typeof(double).Name, defVal, 
                Math.Min(minVal, maxVal), Math.Max(minVal, maxVal))
        { }
        
        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="3"/>
            Value = double.Parse(node.Attributes["value"].Value);
        }
    }

    public class BooleanParameter : AlgorithmParameter<bool>
    {
        public BooleanParameter(string name, string sname) :
            base(name, sname, typeof(bool).Name)
        { }

        public BooleanParameter(string name, string sname, bool defVal) :
            base(name, sname, typeof(bool).Name, defVal, true, false)
        { }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="3"/>
            Value = bool.Parse(node.Attributes["value"].Value);
        }
    }

    public class StringParameter : AlgorithmParameter<string>
    {
        public StringParameter(string name, string sname) :
            base(name, sname, typeof(string).Name)
        { }

        public StringParameter(string name, string sname, string defVal) :
            base(name, sname, typeof(string).Name, defVal, "", "")
        { }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="3"/>
            Value = node.Attributes["value"].Value;
        }
    }

    // Allows choosing one of object from map string -> object
    // Strings are indended to be object names shown in ComboBox
    public class DictionaryParameter : AlgorithmParameter<object>
    {
        public Dictionary<string, object> ValuesMap { get; set; }

        public DictionaryParameter(string name, string sname) :
            base(name, sname, "Dictionary")
        {
            ValuesMap = new Dictionary<string, object>();
        }

        public DictionaryParameter(string name, string sname, object defVal) :
            base(name, sname, "Dictionary", defVal, null, null)
        {
            ValuesMap = new Dictionary<string, object>();
        }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="3"/>
            string objName = node.Attributes["value"].Value;
            Value = ValuesMap[objName];
        }
    }

    // Enables choosing other IParametrizable as parameter
    // For choosen IParametrizable parameter choosing panel should be shown as well
    public class ParametrizedObjectParameter : AlgorithmParameter<IParameterizable>
    {
        public List<IParameterizable> Parameterizables { get; set; }

        public ParametrizedObjectParameter(string name, string sname) :
            base(name, sname, "IParameterizable")
        {
            Parameterizables = new List<IParameterizable>();
        }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" value="parametrizable_name">
            //      <Parameters> <!-- of parametrizable -->
            //      </Parameters>
            //  </Parameter>
            string objName = node.Attributes["value"].Value;
            IParameterizable alg = Parameterizables.Find((p) => { return p.Name == objName; });
            XmlNode algNode = node.FirstChildWithName("Parameters");
            AlgorithmParameter.ReadParametersFromXml(alg.Parameters, algNode);

            Value = alg;
        }
    }
    
    public class Vector2Parameter : AlgorithmParameter<Vector2>
    {
        public Vector2Parameter(string name, string sname) :
            base(name, sname, typeof(Vector2).Name)
        {
            Value = new Vector2();
        }

        public Vector2Parameter(string name, string sname,
            Vector2 defVal, Vector2 minVal, Vector2 maxVal) :
            base(name, sname, typeof(Vector2).Name, defVal, minVal, maxVal)
        {
            Value = new Vector2(defVal);
        }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" x="3" y="3"/>
            Value = new Vector2(
                double.Parse(node.Attributes["x"].Value), 
                double.Parse(node.Attributes["y"].Value));
        }
    }

    public class Vector3Parameter : AlgorithmParameter<Vector3>
    {
        public Vector3Parameter(string name, string sname) :
            base(name, sname, typeof(Vector3).Name)
        {
            Value = new Vector3();
        }

        public Vector3Parameter(string name, string sname,
            Vector3 defVal, Vector3 minVal, Vector3 maxVal) :
            base(name, sname, typeof(Vector3).Name, defVal, minVal, maxVal)
        {
            Value = new Vector3(defVal);
        }

        public override void ReadFromXml(XmlNode node)
        {
            //  <Parameter id="aaa" x="3" y="3" z="3"/>
            Value = new Vector3(
                double.Parse(node.Attributes["x"].Value),
                double.Parse(node.Attributes["y"].Value),
                double.Parse(node.Attributes["z"].Value));
        }
    }
}