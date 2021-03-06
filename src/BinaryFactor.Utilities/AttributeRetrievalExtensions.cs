﻿// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class AttributeRetrievalExtensions
    {
        public static CustomAttributeAccessor CustomAttrs(this Assembly assembly) => new CustomAttributeAccessor(new AssemblyAttributeProvider(assembly));
        public static CustomAttributeAccessor CustomAttrs(this Module module) => new CustomAttributeAccessor(new ModuleAttributeProvider(module));
        public static InheritedCustomAttributeAccessor CustomAttrs(this ParameterInfo parameterInfo) => new InheritedCustomAttributeAccessor(new ParameterAttributeProvider(parameterInfo));
        public static InheritedCustomAttributeAccessor CustomAttrs(this MemberInfo memberInfo) => new InheritedCustomAttributeAccessor(new MemberAttributeProvider(memberInfo));

        public static InheritedCustomAttributeAccessor CustomAttrs(this ICustomAttributeProvider customAttributeProvider)
        {
            return customAttributeProvider switch
            {
                Assembly assembly => new InheritedCustomAttributeAccessor(new AssemblyAttributeProvider(assembly)),
                Module module => new InheritedCustomAttributeAccessor(new ModuleAttributeProvider(module)),
                ParameterInfo parameterInfo => parameterInfo.CustomAttrs(),
                MemberInfo memberInfo => memberInfo.CustomAttrs(),

                _ => new InheritedCustomAttributeAccessor(new FallbackAttributeProvider(customAttributeProvider))
            };
        }

        public static CustomAttributeAccessor CustomAttrs(this Enum enumValue)
        {
            var type = enumValue.GetType();
            var memberInfos = type.GetMember(enumValue.ToString());

            return new CustomAttributeAccessor(new MemberAttributeProvider(memberInfos[0]));
        }
    }

    public class CustomAttributeAccessor
    {
        private readonly AttributeProvider attributeProvider;

        internal CustomAttributeAccessor(AttributeProvider attributeProvider) => this.attributeProvider = attributeProvider;

        public Attribute[] GetAll() => this.attributeProvider.GetCustomAttributes(inherit: false);
        public Attribute[] GetAll(Type attributeType) => this.attributeProvider.GetCustomAttributes(attributeType, inherit: false);
        public TAttribute[] GetAll<TAttribute>() where TAttribute : class => this.attributeProvider.GetCustomAttributes<TAttribute>(inherit: false);
        public dynamic[] GetAll(string attributeTypeName) => this.attributeProvider.GetCustomAttributes(attributeTypeName, inherit: false);

        public Attribute Get(Type attributeType) => GetAll(attributeType).FirstOrDefault();
        public TAttribute Get<TAttribute>() where TAttribute : class => GetAll<TAttribute>().FirstOrDefault();
        public dynamic Get(string attributeTypeName) => GetAll(attributeTypeName).FirstOrDefault();

        public bool Has(Type attributeType) => GetAll(attributeType).Any();
        public bool Has<TAttribute>() where TAttribute : class => GetAll<TAttribute>().Any();
        public bool Has(string attributeTypeName) => GetAll(attributeTypeName).Any();

        public bool Has(Type attributeType, out Attribute attribute) => GetAll(attributeType).AnyWithOut(out attribute);
        public bool Has<TAttribute>(out TAttribute attribute) where TAttribute : class => GetAll<TAttribute>().AnyWithOut(out attribute);
        public bool Has(string attributeTypeName, out dynamic attribute) => GetAll(attributeTypeName).AnyWithOut(out attribute);

        public CustomAttributeData[] GetAllData() => this.attributeProvider.GetCustomAttributesData();
        public CustomAttributeData[] GetAllData(Type attributeType) => this.attributeProvider.GetCustomAttributesData(attributeType);
        public CustomAttributeData[] GetAllData<TAttribute>() where TAttribute : class => this.attributeProvider.GetCustomAttributesData<TAttribute>();
        public CustomAttributeData[] GetAllData(string attributeTypeName) => this.attributeProvider.GetCustomAttributesData(attributeTypeName);

        public CustomAttributeData GetData(Type attributeType) => GetAllData(attributeType).FirstOrDefault();
        public CustomAttributeData GetData<TAttribute>() where TAttribute : class => GetAllData<TAttribute>().FirstOrDefault();
        public CustomAttributeData GetData(string attributeTypeName) => GetAllData(attributeTypeName).FirstOrDefault();
    }

    public class InheritedCustomAttributeAccessor : CustomAttributeAccessor
    {
        private readonly AttributeProvider attributeProvider;

        internal InheritedCustomAttributeAccessor(AttributeProvider attributeProvider) : base(attributeProvider) => this.attributeProvider = attributeProvider;

        public Attribute[] GetAll(bool inherit = false) => this.attributeProvider.GetCustomAttributes(inherit);
        public Attribute[] GetAll(Type attributeType, bool inherit = false) => this.attributeProvider.GetCustomAttributes(attributeType, inherit);
        public TAttribute[] GetAll<TAttribute>(bool inherit = false) where TAttribute : class => this.attributeProvider.GetCustomAttributes<TAttribute>(inherit);
        public dynamic[] GetAll(string attributeTypeName, bool inherit = false) => this.attributeProvider.GetCustomAttributes(attributeTypeName, inherit);

        public Attribute Get(Type attributeType, bool inherit = false) => GetAll(attributeType, inherit).FirstOrDefault();
        public TAttribute Get<TAttribute>(bool inherit = false) where TAttribute : class => GetAll<TAttribute>(inherit).FirstOrDefault();
        public dynamic Get(string attributeTypeName, bool inherit = false) => GetAll(attributeTypeName, inherit).FirstOrDefault();

        public bool Has(Type attributeType, bool inherit = false) => GetAll(attributeType, inherit).Any();
        public bool Has<TAttribute>(bool inherit = false) where TAttribute : class => GetAll<TAttribute>(inherit).Any();
        public bool Has(string attributeTypeName, bool inherit = false) => GetAll(attributeTypeName, inherit).Any();

        public bool Has(Type attributeType, out Attribute attribute, bool inherit = false) => GetAll(attributeType, inherit).AnyWithOut(out attribute);
        public bool Has<TAttribute>(out TAttribute attribute, bool inherit = false) where TAttribute : class => GetAll<TAttribute>(inherit).AnyWithOut(out attribute);
        public bool Has(string attributeTypeName, out dynamic attribute, bool inherit = false) => GetAll(attributeTypeName, inherit).AnyWithOut(out attribute);
    }

    abstract class AttributeProvider
    {
        public Attribute[] GetCustomAttributes(bool inherit) => DoGetCustomAttributes(inherit) ?? new Attribute[0];
        public Attribute[] GetCustomAttributes(Type type, bool inherit) => GetCustomAttributes(inherit).Where(attr => attr.GetType().Is(type)).Cast<Attribute>().ToArray();
        public TAttribute[] GetCustomAttributes<TAttribute>(bool inherit) where TAttribute : class => GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().ToArray();
        public dynamic[] GetCustomAttributes(string attributeTypeName, bool inherit) => GetCustomAttributes(inherit).Where(attr => attr.GetType().FullName == attributeTypeName).ToArray();

        public CustomAttributeData[] GetCustomAttributesData() => DoGetCustomAttributesData()?.ToArray() ?? new CustomAttributeData[0];
        public CustomAttributeData[] GetCustomAttributesData(Type type) => GetCustomAttributesData().Where(attrData => attrData.AttributeType.Is(type)).ToArray();
        public CustomAttributeData[] GetCustomAttributesData<TAttribute>() => GetCustomAttributesData().Where(attrData => attrData.AttributeType.Is<TAttribute>()).ToArray();
        public CustomAttributeData[] GetCustomAttributesData(string attributeTypeName) => GetCustomAttributesData().Where(attrData => attrData.AttributeType.FullName == attributeTypeName).ToArray();

        protected abstract Attribute[] DoGetCustomAttributes(bool inherit);
        protected abstract IList<CustomAttributeData> DoGetCustomAttributesData();
    }

    class ParameterAttributeProvider : AttributeProvider
    {
        private readonly ParameterInfo parameter;

        public ParameterAttributeProvider(ParameterInfo parameter) => this.parameter = parameter;

        protected override Attribute[] DoGetCustomAttributes(bool inherit) => Attribute.GetCustomAttributes(this.parameter, inherit);

        protected override IList<CustomAttributeData> DoGetCustomAttributesData() => this.parameter.GetCustomAttributesData();
    }

    class AssemblyAttributeProvider : AttributeProvider
    {
        private readonly Assembly assembly;

        public AssemblyAttributeProvider(Assembly assembly) => this.assembly = assembly;

        protected override Attribute[] DoGetCustomAttributes(bool inherit) => Attribute.GetCustomAttributes(this.assembly, inherit);

        protected override IList<CustomAttributeData> DoGetCustomAttributesData() => this.assembly.GetCustomAttributesData();
    }

    class ModuleAttributeProvider : AttributeProvider
    {
        private readonly Module module;

        public ModuleAttributeProvider(Module module) => this.module = module;

        protected override Attribute[] DoGetCustomAttributes(bool inherit) => Attribute.GetCustomAttributes(this.module, inherit);

        protected override IList<CustomAttributeData> DoGetCustomAttributesData() => this.module.GetCustomAttributesData();
    }

    class MemberAttributeProvider : AttributeProvider
    {
        private readonly MemberInfo member;

        public MemberAttributeProvider(MemberInfo member) => this.member = member;

        protected override Attribute[] DoGetCustomAttributes(bool inherit) => Attribute.GetCustomAttributes(this.member, inherit);

        protected override IList<CustomAttributeData> DoGetCustomAttributesData() => this.member.GetCustomAttributesData();
    }

    class FallbackAttributeProvider : AttributeProvider
    {
        private readonly ICustomAttributeProvider customAttributeProvider;

        public FallbackAttributeProvider(ICustomAttributeProvider customAttributeProvider) => this.customAttributeProvider = customAttributeProvider;

        protected override Attribute[] DoGetCustomAttributes(bool inherit) => this.customAttributeProvider.GetCustomAttributes(inherit).Cast<Attribute>().ToArray();

        protected override IList<CustomAttributeData> DoGetCustomAttributesData() => throw new NotSupportedException("Getting custom attribute data is only supported for Assembly, Module, MemberInfo and ParameterInfo custom attribute providers");
    }

    static class AnyWithOutParamEnumerableExtension
    {
        public static bool AnyWithOut<T>(this IEnumerable<T> enumerable, out T firstOrDefault)
        {
            firstOrDefault = enumerable.FirstOrDefault();
            return enumerable.Any();
        }
    }
}