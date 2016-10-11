﻿//
// This source code is released under the GPL License; Please read license.md file for more details.
//
using Mono.Cecil;
using NUnit.Framework;
using OpenCover.UI.Model.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenCover.UI.TestDiscoverer.NUnit
{
	/// <summary>
	/// Discovers tests in the given dlls.
	/// </summary>
	internal class NUnitDiscoverer : DiscovererBase
	{
		/// <summary>
        /// Initializes a new instance of the <see cref="NUnitDiscoverer"/> class.
		/// </summary>
		/// <param name="dlls">The DLLS.</param>
        public NUnitDiscoverer(IEnumerable<string> dlls)
            : base(dlls)
        {

        }
        /// <summary>
        /// Recursively loops through the typeDefinition to search for MSTest TestClassAttribute
        /// and returns the found TestClasses in the list.
        /// </summary>
        /// <param name="typeDefinition">A typeDefinition contains in the test assembly, can have nested types</param>
        /// <param name="dll">the dll being worked on, just being passed through</param>
        /// <returns></returns>
        public List<TestClass> FindNunitTestClassInType(TypeDefinition typeDefinition, string dll)
        {
            List<TestClass> testClasses = new List<TestClass>();

            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                List<TestClass> subTestClasses = FindNunitTestClassInType(nestedType, dll); // recursive call
                if (subTestClasses != null)
                {
                    testClasses.AddRange(subTestClasses);
                }
            }

            bool isNunitTest = false;
            var customAttributes = typeDefinition.CustomAttributes;
            if (customAttributes != null)
            {
                isNunitTest = IsNUnitTest(typeDefinition);
                /*isNunitTest = typeDefinition.CustomAttributes != null &&
                           typeDefinition.CustomAttributes.Any(
                               attribute => attribute.AttributeType.FullName == typeof(TestFixtureAttribute).FullName);*/
            }
            if (isNunitTest)
            {
                AddTestClass(dll, typeDefinition, testClasses);
            }
            return testClasses;
        }


        protected override List<TestClass> DiscoverTestsInAssembly(string dllPath, AssemblyDefinition assembly)
	    {
            var classes2 = new List<TestClass>();
            foreach (var type in assembly.MainModule.Types)
            {
                classes2.AddRange(FindNunitTestClassInType(type, dllPath));
            }
            return classes2;
        }

        private void AddTestClass(string dll, TypeDefinition type, List<TestClass> classes2)
        {
            string nameSpace = GetNameSpace(type);
            var TestClass = new TestClass
            {
                DLLPath = dll,
                Name = type.Name,
                Namespace = nameSpace,
                TestType = TestType.NUnit
            };

            TestClass.TestMethods = DiscoverTestsInClass(type, TestClass);
            classes2.Add(TestClass);
        }

     

	    /// <summary>
        /// Discovers the tests in the Assembly.
        /// </summary>
        /// <param name="dllPath">The path to the DLL.</param>
        /// <param name="assembly">The loaded Assembly.</param>
        /// <returns>Tests in the Assembly</returns>
        /* protected override List<TestClass> DiscoverTestsInAssembly(string dllPath, AssemblyDefinition assembly)
         {
             bool hasNUnitReference = AssemblyHasReferenceTo(assembly, "nunit.framework");

             if (!hasNUnitReference)
             {
                 return new List<TestClass>();
             }

             var classes = new List<TestClass>();
             foreach (var type in assembly.MainModule.Types)
             {
                 bool isNUnitTest = false;

                 try
                 {
                     isNUnitTest = IsNUnitTest(type);
                 }
                 catch { }

                 if (isNUnitTest)
                 {
                     var TestClass = new TestClass
                     {
                         DLLPath = dllPath,
                         Name = type.Name,
                         Namespace = type.Namespace,
                         TestType = TestType.NUnit
                     };

                     TestClass.TestMethods = DiscoverTestsInClass(type, TestClass);

                     classes.Add(TestClass);
                 }
             }
             return classes;
         }
         */
        /// <summary>
        /// Determines whether the Type has TestFixtrue Attribute on itself or on one of its parents
        /// </summary>
        /// <param name="type">The type.</param>
        private bool IsNUnitTest(TypeDefinition type)
		{
			if (type == null)
			{
				return false;
			}

			if (type.CustomAttributes != null && type.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == typeof(TestFixtureAttribute).FullName))
			{
				return true;
			}

			if (type.BaseType != null && type.BaseType is TypeDefinition)
			{
				return IsNUnitTest(type.BaseType as TypeDefinition);
			}

            if (type.Methods.Any(method => method.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == typeof(TestAttribute).FullName))) 
            {
                return true;
            }

            return false;
		}

		/// <summary>
		/// Discovers the tests in class.
		/// </summary>
		/// <param name="type">Type of the class.</param>
		/// <returns>Tests in the class</returns>
		private TestMethod[] DiscoverTestsInClass(TypeDefinition type, TestClass @class)
		{
			var tests = new List<TestMethod>();

			foreach (var method in type.Methods)
			{
				bool isTestMethod = false;
				var trait = new List<string>();

				try
				{
                    foreach (var attribute in method.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == typeof(TestAttribute).FullName 
                            || attribute.AttributeType.FullName == typeof(TestCaseAttribute).FullName)
                        {
                            isTestMethod = true;
                        }

                        AddTraits(trait, attribute, typeof(CategoryAttribute));
                    }
				}
				catch { }

				if (isTestMethod)
				{
					TestMethod testMethod = new TestMethod();
					testMethod.Name = method.Name;
					testMethod.Traits = trait.Count > 0 ? trait.ToArray() : new[] { "No Traits" };
					tests.Add(testMethod);
				}
			}

			return tests.ToArray();
		}

	}
}
