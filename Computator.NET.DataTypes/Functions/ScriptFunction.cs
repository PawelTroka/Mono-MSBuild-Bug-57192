using System;
using System.Reflection;

namespace Computator.NET.DataTypes
{
    public class ScriptFunction : BaseFunction
    {
        public ScriptFunction(Delegate function) : base(function)

        {
            FunctionType = FunctionType.Scripting;
        }

        public void Evaluate(Action<string> consoleCallback)
        {
            try
            {
                ((Action<Action<string>>) _function)(consoleCallback);
            }
            catch (Exception exception)
            {
                var message = "Calculation Error, details:" + Environment.NewLine + exception.Message;
               throw new CalculationException(message, exception);
            }
        }
    }
}