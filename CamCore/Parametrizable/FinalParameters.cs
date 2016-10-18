using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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
    }

    public class BooleanParameter : AlgorithmParameter<bool>
    {
        public BooleanParameter(string name, string sname) :
            base(name, sname, typeof(bool).Name)
        { }

        public BooleanParameter(string name, string sname, bool defVal) :
            base(name, sname, typeof(bool).Name, defVal, true, false)
        { }
    }

    public class StringParameter : AlgorithmParameter<string>
    {
        public StringParameter(string name, string sname) :
            base(name, sname, typeof(string).Name)
        { }

        public StringParameter(string name, string sname, string defVal) :
            base(name, sname, typeof(string).Name, defVal, "", "")
        { }
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
    }
}