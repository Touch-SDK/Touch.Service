using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Web.Configuration;

namespace Touch.ServiceModel.Configuration
{
    static public class ConfigurationResolver
    {
        public static Binding ResolveBinding(string name)
        {
            var section = GetBindingsSection();

            foreach (var bindingCollection in section.BindingCollections)
            {
                for (var i = 0; i < bindingCollection.ConfiguredBindings.Count; i++)
                {
                    var bindingElement = bindingCollection.ConfiguredBindings[i];

                    if (bindingElement.Name != name) continue;

                    var binding = (Binding) Activator.CreateInstance(bindingCollection.BindingType);
                    binding.Name = bindingElement.Name;
                    bindingElement.ApplyConfiguration(binding);

                    return binding;
                }
            }

            return null;
        }

        public static List<IEndpointBehavior> ResolveEndpointBehavior(string name)
        {
            var section = GetBehaviorsSection();

            for (var i = 0; i < section.EndpointBehaviors.Count; i++)
            {
                var behaviorCollectionElement = section.EndpointBehaviors[i];

                if (behaviorCollectionElement.Name == name)
                {
                    var endpointBehaviors = new List<IEndpointBehavior>();

                    foreach (var behaviorExtension in behaviorCollectionElement)
                    {
                        var extension = behaviorExtension.GetType().InvokeMember(
                            "CreateBehavior",
                            BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            behaviorExtension,
                            null
                        );

                        endpointBehaviors.Add((IEndpointBehavior) extension);
                    }

                    return endpointBehaviors;
                }
            }

            return null;
        }

        public static List<IServiceBehavior> ResolveServiceBehavior(string name)
        {
            var section = GetBehaviorsSection();

            for (var i = 0; i < section.ServiceBehaviors.Count; i++)
            {
                var behaviorCollectionElement = section.ServiceBehaviors[i];

                if (behaviorCollectionElement.Name == name)
                {
                    var serviceBehaviors = new List<IServiceBehavior>();

                    foreach (var behaviorExtension in behaviorCollectionElement)
                    {
                        var extension = behaviorExtension.GetType().InvokeMember(
                            "CreateBehavior",
                            BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            behaviorExtension,
                            null
                        );

                        serviceBehaviors.Add((IServiceBehavior)extension);
                    }

                    return serviceBehaviors;
                }
            }

            return null;
        }

        private static BindingsSection GetBindingsSection()
        {
            var config = WebConfigurationManager.OpenWebConfiguration("~/web.config");
            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);

            return serviceModel != null
                ? serviceModel.Bindings
                : null;
        }

        private static BehaviorsSection GetBehaviorsSection()
        {
            var config = WebConfigurationManager.OpenWebConfiguration("~/web.config");
            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);

            return serviceModel != null
                ? serviceModel.Behaviors
                : null;
        }
    }
}
