using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CamCore
{
    // Interface for arbitrary algorithm etc. that can be parametrized using list of ProcessorParameters
    public interface IParametrizedProcessor
    {
        List<ProcessorParameter> Parameters { get; }
        // Creates and fills with default values list of parameters
        void InitParameters();
        // Sets new parameters 
        void UpdateParameters();
    }

    /*
        Set of parameters used by image processors (ie. detectors)
        Contains definitions of parameter and actual values
        Class used to pass info on necessary params outside processor (ie. for input)
        as well as to recieve values of them in universal way for every processor 
    */
    public class ProcessorParameter
    {
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public string TypeName { get; private set; }
        public object DefaultValue { get; private set; }
        public object MinValue { get; private set; }
        public object MaxValue { get; private set; }
        public object ActualValue { get; set; }

        public ProcessorParameter(string name, string sname, string typename, object defval, object minval, object maxval)
        {
            Name = name;
            ShortName = sname;
            TypeName = typename;
            DefaultValue = defval;
            MinValue = minval;
            MaxValue = maxval;
            ActualValue = defval;
        }

        public static ProcessorParameter FindParameter(string name, List<ProcessorParameter> paramsList)
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

        public static object FindValue(string name, List<ProcessorParameter> paramsList)
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
    }
}