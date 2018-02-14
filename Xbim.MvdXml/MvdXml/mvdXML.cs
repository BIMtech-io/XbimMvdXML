﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using log4net;
using Xbim.MvdXml.DataManagement;

// ReSharper disable once CheckNamespace
namespace Xbim.MvdXml
{
    // ReSharper disable once InconsistentNaming
    public partial class mvdXML: IUnique, IReference
    {
        private static readonly ILog Log = LogManager.GetLogger("Xbim.MvdXml.mvdXML");

        private static XmlSerializer _serializer;
        
        private static XmlSerializer Serializer
        {
            get
            {
                if ((_serializer == null))
                {
                    _serializer = new XmlSerializer(typeof(mvdXML));
                }
                return _serializer;
            }
        }
        
        /// <summary>
        /// Returns an mvdXml from a serialized string
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static mvdXML Deserialize(string xml)
        {
            StringReader stringReader = null;
            try
            {
                using (stringReader = new StringReader(xml))
                {
                    using (var xreader = XmlReader.Create(stringReader))
                    {
                        return Deserialize(xreader);
                    }
                }
            }
            finally
            {
                stringReader?.Dispose();
            }
        }

        private static mvdXML Deserialize(XmlReader xreader)
        {
            var ser = (mvdXML) (Serializer.Deserialize(xreader));
            return ser;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ConceptTemplate> GetAllConceptTemplates()
        {
            foreach (var tmplt in Templates)
            {
                foreach (var subtmplt in tmplt.GetTemplatesTree())
                {
                    yield return subtmplt;
                }
            }
        }
        
        /// <summary>
        /// Transverse the root of views and roots to list all Concepts.
        /// </summary>
        /// <returns>Enumerable of Concepts</returns>
        public IEnumerable<Concept> GetAllConcepts()
        {
            //simplified from
            //foreach (var view in _mvd.Views)
            //{
            //    foreach (var conceptRoot in view.Roots)
            //    {
            //        foreach (var concept in conceptRoot.Concepts)
            //            yield return concept;
            return from view in Views from conceptRoot in view.Roots from concept in conceptRoot.Concepts select concept;
        }

        public IEnumerable<ConceptRoot> GetAllConceptsRoots()
        {
            return Views.SelectMany(view => view.Roots);
        }



        /// <summary>
        /// Loads an mvdXML deserializing it from a file.
        /// </summary>
        /// <param name="fileName">the filen name to load</param>
        /// <param name="attemptRecovery">Wnen true attempts to fix file before opening, if needed.</param>
        /// <returns>deserialized mvdXML</returns>
        public static mvdXML LoadFromFile(string fileName, bool attemptRecovery = true)
        {
            if (attemptRecovery)
            {
                var comp = TestCompatibility(fileName);
                if (comp == CompatibilityResult.InvalidNameSpace)
                {
                    Log.Info($"Attempt to fix namespace in xml file [{fileName}].");
                    var newName = Path.GetTempFileName();
                    if (FixNamespace(fileName, newName))
                    {
                        Log.Info($"Succesfully fixed namespace in xml file [{fileName}].");
                        return LoadFromFile(newName, false);
                    }
                    else
                    {
                        Log.Error($"Attempt to fix namespace in invalid xml file [{fileName}] failed.");
                    }
                }
            }

            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var xReader = XmlReader.Create(file))
                {
                    var des = Deserialize(xReader);
                    return des;
                }
            }
        }

        [XmlIgnore()] private bool _notifiedMissingUuid = false;

        [XmlIgnore()]
        private Dictionary<string, ConceptTemplate> _conceptTemplates;

        /// <summary>
        /// Returns a ConceptTemplate existing in the mvdXML given a string id.
        /// </summary>
        /// <param name="refUuid">The string id of the ConceptTemplate to retrieve</param>
        /// <returns>Null if not found.</returns>
        public ConceptTemplate GetConceptTemplate(string refUuid)
        {
            if (_conceptTemplates == null)
            {
                _conceptTemplates = new Dictionary<string, ConceptTemplate>();
                if (!_conceptTemplates.ContainsKey(refUuid))
                {
                    foreach (var conceptTemplate in Templates)
                    {
                        foreach (var subTemp in conceptTemplate.GetTemplatesTree())
                        {
                            if (!_conceptTemplates.ContainsKey(subTemp.uuid))
                                _conceptTemplates.Add(subTemp.uuid, subTemp);
                        }
                    }
                }
            }
            ConceptTemplate ret;
            var found = _conceptTemplates.TryGetValue(refUuid, out ret) ;
            if (found || _notifiedMissingUuid)
                return ret;
            Log.Error("Some UUID references could not be found in the file. Run integrity tests for details.");
            _notifiedMissingUuid = true;
            return null;
        }

        [XmlIgnore()]
        internal MvdEngine Engine;

        /// <summary>
        /// gets the Engine that might be referencing this mvdXML  
        /// </summary>
        /// <returns>null if no engine built on the instance.</returns>
        public MvdEngine GetEngine()
        {
            return Engine;
        }

        /// <summary>
        /// Provides feedback to evaluate the compatility of a candidate mvdXML file with the current schema
        /// </summary>
        public enum CompatibilityResult
        {
            /// <summary>
            /// The file can be loaded with no issues
            /// </summary>
            NoProblem,
            /// <summary>
            /// The namespace of the Document node is invalid
            /// </summary>
            InvalidNameSpace,
            /// <summary>
            /// The file has schema issues.
            /// </summary>
            InvalidSchema,
            /// <summary>
            /// The node cannot be loaded because of an undetected reason.
            /// </summary>
            Unexpected,
        }

        private class MyCustomResolver : XmlUrlResolver
        {
            private const string MYRESOURCENAMESPACE = "MyNamespace.{0}";
            private Assembly resourceAssembly = null;

            public MyCustomResolver(Assembly resourceAssembly)
            {
                if (resourceAssembly == null)
                    throw new ArgumentNullException("resourceAssembly must not be null");
                this.resourceAssembly = resourceAssembly;
            }

            override public object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                if (absoluteUri.IsFile)
                {
                    string file = Path.GetFileName(absoluteUri.AbsolutePath);
                    Stream stream = resourceAssembly.GetManifestResourceStream(
                        String.Format(MYRESOURCENAMESPACE, file));
                    return stream;
                }
                return null;
            }
        }

        public static CompatibilityResult TestSchemaAdherence(string fileName)
        {

            var a = Assembly.GetExecutingAssembly();
            var stream = a.GetManifestResourceStream("Xbim.MvdXml.Schema.mvdXML_V1.1.xsd");

            var x = XmlSchema.Read(stream, new ValidationEventHandler(SchemaValidationEventHandler));

            //x.Compile(
            //    new ValidationEventHandler(SchemaValidationEventHandler),
            //    new MyCustomResolver(a));

            //schemas.Add(x);
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(x);

            XDocument doc1 = XDocument.Load(fileName);

            bool errors = false;
            doc1.Validate(schemas, (sender, e) =>
            {
                Debug.WriteLine(e.Message);
                errors = true;
            }, true);

            if (errors)
                return CompatibilityResult.InvalidSchema;
            return  CompatibilityResult.NoProblem;
        }

        private static void SchemaValidationEventHandler(object sender, ValidationEventArgs e)
        {
           
        }


        /// <summary>
        /// Evaluates the compatility of a candidate mvdXML file with the current schema
        /// </summary>
        /// <param name="fileName">the candidate file name</param>
        /// <returns><see cref="CompatibilityResult"/></returns>
        public static CompatibilityResult TestCompatibility(string fileName)
        {
            var d = new XmlDocument();
            d.Load(fileName);
            if (d.DocumentElement == null)
                return CompatibilityResult.Unexpected;
            return d.DocumentElement.NamespaceURI != ExpectedNameSpace 
                ? CompatibilityResult.InvalidNameSpace 
                : CompatibilityResult.NoProblem;
        }

        private const string ExpectedNameSpace = "http://buildingsmart-tech.org/mvd/XML/1.1";

        /// <summary>
        /// Replaces the namespace of the mvdXML element with the expected one.
        /// </summary>
        /// <param name="fileName">File to fix.</param>
        /// <param name="newName">Filename of the fixed file.</param>
        /// <returns></returns>
        public static bool FixNamespace(string fileName, string newName)
        {
            try
            {
                var d = new XmlDocument();
                d.Load(fileName);
                var e = d.DocumentElement;
                if (e == null)
                    return false;
                e.RemoveAttribute("xmlns");
                e.SetAttribute("xmlns", ExpectedNameSpace);

                d.Save(newName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error cought while attempting namespace fix", ex);
                return false;
            }
        }

        /// <summary>
        /// provides access to the underlying Uuid string
        /// </summary>
        /// <returns>a string</returns>
        public string GetUuid()
        {
            return uuid;
        }

        IEnumerable<ReferenceConstraint> IReference.DirectReferences()
        {
            yield break;
        }

        IEnumerable<ReferenceConstraint> IReference.AllReferences()
        {
            // templates
            foreach (IReference conceptTemplate in Templates)
            {
                foreach (var sub in conceptTemplate.AllReferences())
                {
                    yield return sub;
                }
            }
            // Views
            foreach (IReference view in Views)
            {
                foreach (var sub in view.AllReferences())
                {
                    yield return sub;
                }
            }

        }
    }
}
