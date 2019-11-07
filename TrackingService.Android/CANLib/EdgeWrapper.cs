using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Dynamic;

namespace CANLib
{
    public class Startup
    {
        public async Task<object> Invoke(dynamic input)
        {
            IEnumerable<MethodInfo> methodInfos = typeof(CNXCANMsgHelper).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var dict = input as IDictionary<string, object>;
            if (dict == null) throw new Exception("Input value must be a adynamic object");
            string methodName = dict.ContainsKey("method") ? (string)dict["method"] : null;
            if (methodName == null)
            {
                input.time = DateTime.Now;
                return input;
            }
            else if (methodName == "GetMethods")
            {
                string[] methods = methodInfos.Select(x => x.Name).Distinct().ToArray();
                return methods;
            }
            methodInfos = methodInfos.Where(x => x.Name == methodName);
            if (methodInfos.Count() == 0) throw new Exception("No method named " + methodName + " is defined");
            bool correctMethod;
            MethodInfo methodInfo = null;
            foreach (var mi in methodInfos)
            {
                correctMethod = true;
                var ps = mi.GetParameters();
                foreach (var p in ps)
                    if (!argGiven(dict, p, ps)) {
                        if (Nullable.GetUnderlyingType(p.ParameterType) == null && !p.IsOut)
                        {
                            correctMethod = false;
                            break;
                        }
                    } else if (!isType(arg(dict, p, ps), p.ParameterType))
                    {
                        correctMethod = false;
                        break;
                    }
                if (correctMethod)
                {
                    methodInfo = mi;
                    break;
                }
            }
            if (methodInfo == null) throw new Exception("No method named " + methodName + " is defined with arguments of the given type(s)");
            var args = methodInfo.GetParameters();
            object[] argList = new object[args.Length];
            bool hasOuts = false;
            for (int i = 0; i < args.Length; i++)
            {
                argList[i] = argGiven(dict, i, args) ?
                    (args[i].ParameterType == typeof(CANFrame) ? makeCANFrame(arg(dict, i, args)) : arg(dict, i, args)) :
                    null;
                hasOuts |= args[i].IsOut;
            }
            if (hasOuts)
            {
                dynamic returnVal = new ExpandoObject();

                if (methodInfo.ReturnType == typeof(void))
                    methodInfo.Invoke(null, argList);
                else if (methodInfo.ReturnType == typeof(bool))
                    returnVal.success = methodInfo.Invoke(null, argList);
                else
                    returnVal.returnVal = methodInfo.Invoke(null, argList);

                for (int i = 0; i < args.Length; i++)
                    if (args[i].IsOut)
                        ((IDictionary<string, object>)returnVal)[args[i].Name] = argList[i];
                return returnVal;
            }
            return methodInfo.Invoke(null, argList);

        }

        static bool isType(dynamic x, Type t)
        {
            if (t == typeof(CANFrame))
            {
                if (x is CANFrame) return true;
                var d = (IDictionary<string, object>)((ExpandoObject)x);
                return d.ContainsKey("MailboxId") && d["MailboxId"] is int && (int)d["MailboxId"] > 0 &&
                        d.ContainsKey("DataLength") && d["DataLength"] is int &&
                        d.ContainsKey("Data") && d["Data"] is byte[] &&
                        d.ContainsKey("WireFormatArray") && d["WireFormatArray"] is byte[] &&
                        d.ContainsKey("Bytes") && d["Bytes"] is byte[];

            }
            return t.IsInstanceOfType(x) || 
                    ((t == typeof(byte) || t == typeof(ushort)) && typeof(int).IsInstanceOfType(x) && (uint)((int)x) < 256);
        }

        static bool argGiven(IDictionary<string, object> dict, string name, ParameterInfo[] args)
        {
            if (dict.ContainsKey("args"))
                return ((object[])dict["args"]).Length > Array.IndexOf(args, Array.Find(args, x => x.Name == name));
            else
                return dict.ContainsKey(name);
        }

        static bool argGiven(IDictionary<string, object> dict, ParameterInfo arg, ParameterInfo[] args)
        {
            if (dict.ContainsKey("args"))
                return ((object[])dict["args"]).Length > Array.IndexOf(args, arg);
            else
                return dict.ContainsKey(arg.Name);
        }

        static bool argGiven(IDictionary<string, object> dict, int i, ParameterInfo[] args)
        {
            if (dict.ContainsKey("args"))
                return ((object[])dict["args"]).Length > i;
            else
                return dict.ContainsKey(args[i].Name);
        }

        static object arg(IDictionary<string, object> dict, string name, ParameterInfo[] args)
        {
            if (dict.ContainsKey("args"))
                return ((object[])dict["args"])[Array.IndexOf(args, Array.Find(args, x => x.Name == name))];
            else
                return dict[name];
        }

        static object arg(IDictionary<string, object> dict, ParameterInfo arg, ParameterInfo[] args)
        {
            if (dict.ContainsKey("args"))
                return ((object[])dict["args"])[Array.IndexOf(args, arg)];
            else
                return dict[arg.Name];
        }

        static object arg(IDictionary<string, object> dict, int i, ParameterInfo[] args)
        {
            if (dict.ContainsKey("args"))
                return ((object[])dict["args"])[i];
            else
                return dict[args[i].Name];
        }

        static CANFrame makeCANFrame(dynamic x)
        {
            var cf = new CANFrame()
            {
                MailboxId = (uint)x.MailboxId
            };
            cf.Data = (byte[])x.Data;
            return cf;
            
        }
    }
}
