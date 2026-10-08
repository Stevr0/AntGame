using System;
using System.Reflection;
using NUnit.Framework;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Runs the focused motor-state regression through Unity Test Framework.
    /// Reflection avoids moving the existing runtime scripts out of Unity's
    /// predefined Assembly-CSharp during a narrowly scoped bug fix.
    /// </summary>
    public sealed class AntSurfaceMotorDeadlockTests
    {
        [Test]
        public void CornerTransitionFailureAndReversal_DoNotLeaveMotorLocked()
        {
            Type validatorType = Type.GetType(
                "AntColony.EditorTools.AntSurfaceMotorRegressionValidation, Assembly-CSharp-Editor");
            Assert.That(validatorType, Is.Not.Null, "Regression validator type was not found.");

            MethodInfo validationMethod = validatorType.GetMethod(
                "ValidateCornerDeadlockRegression",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(validationMethod, Is.Not.Null, "Regression validation method was not found.");

            try
            {
                validationMethod.Invoke(null, null);
            }
            catch (TargetInvocationException exception)
            {
                // Surface the validator's real assertion message in test output.
                throw exception.InnerException ?? exception;
            }
        }
    }
}
