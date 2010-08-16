using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Xsd2Code.Library.Properties;

namespace Xsd2Code.Library.Extensions
{
    internal class TrackingChangesExtention
    {

        /// <summary>
        /// Generates the tracking changes classes.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CodeTypeDeclaration> GenerateTrackingChangesClasses()
        {
            var classList = new List<CodeTypeDeclaration>();

            classList.Add(CreateObjectsToCollectionProperties("ObjectsOriginalFromCollectionProperties", "ObjectsOriginalToCollectionProperties", "OriginalObjectsForProperty", "CollectionPropertyName", "OriginalObjects", "Dictionary<string, ObjectList>"));
            classList.Add(CreateObjectsToCollectionProperties("ObjectsAddedToCollectionProperties", "ObjectsAddedToCollectionProperties", "AddedObjectsForProperty", "CollectionPropertyName", "AddedObjects", "Dictionary<string, ObjectList>"));
            classList.Add(CreateObjectsToCollectionProperties("ObjectsRemovedFromCollectionProperties", "ObjectsRemovedFromCollectionProperties", "DeletedObjectsForProperty", "CollectionPropertyName", "DeletedObjects", "Dictionary<string, ObjectList>"));
            classList.Add(CreateObjectsToCollectionProperties("PropertyValueStatesDictionary", "PropertyValueStatesDictionary", "PropertyValueStates", "Name", "PropertyValueState", "Dictionary<string, PropertyValueState>"));

            // TrackableCollection
            var trackableCollectionClass = new CodeTypeDeclaration("TrackableCollection")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public,
            };
            trackableCollectionClass.TypeParameters.Add(new CodeTypeParameter("T"));
            var ctr = new CodeTypeReference("ObservableCollection");
            ctr.TypeArguments.Add("T");

            trackableCollectionClass.BaseTypes.Add(ctr);

            var codeTemplate = new CodeSnippetTypeMember();
            codeTemplate.Text = Resources.TrackableCollection_cs;
            trackableCollectionClass.Members.Add(codeTemplate);
            trackableCollectionClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "TrackableCollection class"));
            trackableCollectionClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "TrackableCollection"));
            classList.Add(trackableCollectionClass);

            // ObjectChangeTracker class
            var trackableClass = new CodeTypeDeclaration("ObjectChangeTracker")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public,
            };
            trackableClass.BaseTypes.Add(typeof(INotifyPropertyChanged));
            trackableClass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("DataContract"),
                                                                        new[]
                                                                            {
                                                                                new CodeAttributeArgument(
                                                                                    "IsReference",
                                                                                    new CodeSnippetExpression(
                                                                                        "true"))
                                                                            }));
            var cm = new CodeSnippetTypeMember();
            cm.Text = Resources.ObjectChangeTracker_cs;
            trackableClass.Members.Add(cm);
            trackableClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Tracking changes class"));
            trackableClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "Tracking changes class"));
            classList.Add(trackableClass);

            var notifyTrackableDelegate = new CodeTypeDelegate("NotifyTrackableCollectionChangedEventHandler");
            notifyTrackableDelegate.Parameters.Add(new CodeParameterDeclarationExpression("System.Object", "sender"));
            notifyTrackableDelegate.Parameters.Add(new CodeParameterDeclarationExpression("NotifyCollectionChangedEventArgs", "e"));
            notifyTrackableDelegate.Parameters.Add(new CodeParameterDeclarationExpression("system.string", "propertyName"));
            notifyTrackableDelegate.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "NotifyTrackableCollectionChangedEventHandler class"));
            notifyTrackableDelegate.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "NotifyTrackableCollectionChangedEventHandler"));
            classList.Add(notifyTrackableDelegate);

            // enum ObjectState
            var objectStateenum = new CodeTypeDeclaration("ObjectState")
                            {
                                IsEnum = true,
                                TypeAttributes = TypeAttributes.Public
                            };
            // Creates the enum member
            objectStateenum.Members.Add(new CodeMemberField(typeof(int), "Unchanged"));
            objectStateenum.Members.Add(new CodeMemberField(typeof(int), "Added"));
            objectStateenum.Members.Add(new CodeMemberField(typeof(int), "Modified"));
            objectStateenum.Members.Add(new CodeMemberField(typeof(int), "Deleted"));
            objectStateenum.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "ObjectState enum"));
            objectStateenum.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "ObjectState"));
            classList.Add(objectStateenum);


            // ObjectList class
            var ObjectListClass = new CodeTypeDeclaration("ObjectList")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public,
            };

            ctr = new CodeTypeReference("List");
            ctr.TypeArguments.Add(typeof(object));
            ObjectListClass.BaseTypes.Add(ctr);
            ObjectListClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "ObjectList class"));
            ObjectListClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "ObjectList"));
            classList.Add(ObjectListClass);

            // ObjectStateChangingEventArgs class
            var objectStateChangingEventArgsClass = new CodeTypeDeclaration("ObjectStateChangingEventArgs")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public,
            };

            ctr = new CodeTypeReference("EventArgs");
            objectStateChangingEventArgsClass.BaseTypes.Add(ctr);

            codeTemplate = new CodeSnippetTypeMember();
            codeTemplate.Text = Resources.ObjectStateChangingEventArgs_cs;
            objectStateChangingEventArgsClass.Members.Add(codeTemplate);
            objectStateChangingEventArgsClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "ObjectStateChangingEventArgs class"));
            objectStateChangingEventArgsClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "ObjectStateChangingEventArgs"));
            classList.Add(objectStateChangingEventArgsClass);

            // ObjectChangeTracker class
            var propertyValueStateClass = new CodeTypeDeclaration("PropertyValueState")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public,
            };
            cm = new CodeSnippetTypeMember();
            cm.Text = Resources.PropertyValueState_cs;
            propertyValueStateClass.Members.Add(cm);
            propertyValueStateClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "PropertyValueState class"));
            propertyValueStateClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "PropertyValueState class"));
            classList.Add(propertyValueStateClass);

            return classList;
        }

        private static CodeTypeDeclaration CreateObjectsToCollectionProperties(string className, string nameAttributeValue, string itemNameValue, string keyNameValue, string valueNameValue, string valueType)
        {
            var objectsOriginalFromCollectionPropertiesClass = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public,
            };

            objectsOriginalFromCollectionPropertiesClass.BaseTypes.Add(valueType);
            objectsOriginalFromCollectionPropertiesClass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("CollectionDataContract"),
                                                                                                           new[]
                                                                                                               {
                                                                                                                   new CodeAttributeArgument("Name",new CodeSnippetExpression(string.Concat("\"", nameAttributeValue, "\""))),
                                                                                                                   new CodeAttributeArgument("ItemName",new CodeSnippetExpression(string.Concat("\"", itemNameValue, "\""))),
                                                                                                                   new CodeAttributeArgument("KeyName",new CodeSnippetExpression(string.Concat("\"", keyNameValue, "\""))),
                                                                                                                   new CodeAttributeArgument("ValueName", new CodeSnippetExpression(string.Concat("\"", valueNameValue, "\"")))
                                                                                                               }));

            objectsOriginalFromCollectionPropertiesClass.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, className));
            objectsOriginalFromCollectionPropertiesClass.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, className));

            return objectsOriginalFromCollectionPropertiesClass;
        }
    }
}
