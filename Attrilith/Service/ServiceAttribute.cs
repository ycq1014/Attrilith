using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Attrilith.Service;

// 定义增强型服务特性体系
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ServiceAttribute : System.Attribute
{
    public ServiceLifetime Lifetime { get; }
    public Type? ServiceType { get; } = null;
    public bool AsSelf { get; }

    // 支持三种注册模式
    public ServiceAttribute(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        bool registerAsSelf = true
    ) : this(null!, lifetime, registerAsSelf)
    {
    }

    public ServiceAttribute(
        Type serviceType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        bool registerAsSelf = true
    )
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
        AsSelf = registerAsSelf;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public abstract class HostedServiceAttribute(bool startImmediately = false) : System.Attribute
{
    public bool RunImmediately { get; } = startImmediately;
}

public class NamingConventionRule(string name, ServiceLifetime lifetime)
{
    public readonly string Name = name;

    public readonly ServiceLifetime Lifetime = lifetime;
}

// 可配置的自动注册规则
public class AutoRegisterOptions
{
    // 约定规则配置
    public List<NamingConventionRule> ConventionRules { get; init; } =
    [
        new NamingConventionRule("Service", ServiceLifetime.Singleton),
        new NamingConventionRule("Repository", ServiceLifetime.Singleton)
    ];

    // 排除的类型过滤器
    public Func<Type, bool> TypeFilter { get; set; } =
        t => !t.Name.StartsWith("Temp");

    // 是否使用命名约定注册
    public bool AutoRegisterByConvention { get; init; } = true;

    // 是否使用特性注册
    public bool AutoRegisterByAttribute { get; init; } = true;

    // 是否启用自动注册托管服务
    public bool AutoRegisterHostedServices { get; init; } = true;

    // 是否启用注册接口
    public bool AutoRegisterInterfaces { get; init; } = true;
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartServices(
        this IServiceCollection services)
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        foreach (var assembly in assemblies)
        {
            RegisterByAttribute(services, assembly);
            RegisterHostedServices(services, assembly);
        }

        return services;
    }

    public static IServiceCollection AddSmartServices(
        this IServiceCollection services,
        AutoRegisterOptions options,
        params Assembly?[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = new[] { Assembly.GetExecutingAssembly() };

        foreach (var assembly in assemblies)
        {
            if (assembly == null)
            {
                continue;
            }

            RegisterByConvention(services, assembly, options);
            RegisterByAttribute(services, assembly, options);
            RegisterHostedServices(services, assembly, options);
        }

        return services;
    }

    // 根据命名约定注册服务
    private static void RegisterByConvention(IServiceCollection services, Assembly assembly,
        AutoRegisterOptions? options = null)
    {
        if (options?.AutoRegisterByConvention == false)
        {
            return;
        }

        Dictionary<string, ServiceLifetime> conventionRules = new();
        if (conventionRules == null) throw new ArgumentNullException(nameof(conventionRules));

        if (options == null || options.ConventionRules.Count == 0)
        {
            conventionRules["Service"] = ServiceLifetime.Scoped;
            conventionRules["Repository"] = ServiceLifetime.Singleton;
        }
        else
        {
            foreach (var rule in options.ConventionRules)
            {
                conventionRules[rule.Name] = rule.Lifetime;
            }
        }

        var conventionTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => conventionRules.Keys
                .Any(name => t.Name.EndsWith(name, StringComparison.OrdinalIgnoreCase)))
            .ToList(); // 立即执行

        conventionTypes.ForEach(t =>
        {
            ServiceLifetime? lifetime = null;

            var interfaceType = t.GetInterfaces().FirstOrDefault() ?? t;

            foreach (var name in conventionRules.Keys)
            {
                if (interfaceType.Name.EndsWith(name))
                {
                    lifetime = conventionRules[name];
                }
            }

            if (lifetime == null)
            {
                return;
            }

            services.TryAdd(new ServiceDescriptor(interfaceType, t, lifetime));
        });
    }

    private static void RegisterByAttribute(IServiceCollection services,
        Assembly assembly,
        AutoRegisterOptions? options = null)
    {
        // 参数校验前置
        if (options?.AutoRegisterByAttribute == false)
        {
            return;
        }

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            try
            {
                var serviceAttr = type.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttr == null) continue;

                // 明确服务类型优先级：ServiceType > AsSelf > 第一个接口 > 自身
                var serviceType = serviceAttr.ServiceType ??
                                  (serviceAttr.AsSelf ? type : GetPrimaryInterface(type));

                // 主注册
                RegisterServiceWithLifetime(services, type, serviceType, serviceAttr.Lifetime);

                // 自动接口注册（根据配置决定）
                if (options?.AutoRegisterInterfaces == true)
                {
                    RegisterImplementedInterfaces(services, type, serviceAttr.Lifetime);
                }
            }
            catch (Exception e) when (e is TypeLoadException or InvalidOperationException)
            {
                // Ignore
            }
        }
    }

    // 获取主要接口（可扩展为自定义策略）
    private static Type GetPrimaryInterface(Type implementationType)
    {
        // 优先选择非系统接口
        var interfaces = implementationType.GetInterfaces()
            .ToList();

        return interfaces.FirstOrDefault() ?? implementationType;
    }

    // 带过滤的接口注册
    private static void RegisterImplementedInterfaces(IServiceCollection services,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        foreach (var interfaceType in implementationType.GetInterfaces()
                     .Where(i => !IsExcludedInterface(i)))
        {
            services.TryAdd(new ServiceDescriptor(
                interfaceType,
                implementationType,
                lifetime
            ));
        }
    }

    // 系统接口过滤（可配置化扩展）
    private static bool IsExcludedInterface(Type interfaceType)
    {
        var excludedInterfaces = new[]
        {
            typeof(IDisposable),
            typeof(IAsyncDisposable)
        };

        return excludedInterfaces.Contains(interfaceType) ||
               interfaceType.Namespace?.StartsWith("System") == true;
    }

    // 统一服务注册方法 
    private static void RegisterServiceWithLifetime(
        IServiceCollection services,
        Type implementationType,
        Type serviceType,
        ServiceLifetime lifetime)
    {
        // 处理泛型类型定义（如IRepository<> -> Repository<>）
        if (serviceType.IsGenericTypeDefinition && implementationType.IsGenericTypeDefinition)
        {
            services.TryAdd(new ServiceDescriptor(
                serviceType,
                implementationType,
                lifetime
            ));
        }
        else
        {
            services.TryAdd(new ServiceDescriptor(
                serviceType,
                implementationType,
                lifetime
            ));
        }
    }

    // 注册托管服务
    private static void RegisterHostedServices(IServiceCollection services, Assembly assembly,
        AutoRegisterOptions? options = null)
    {
        if (options?.AutoRegisterHostedServices == false)
        {
            return;
        }

        var hostedServices = assembly.GetTypes()
            .Where(t => !t.IsAbstract &&
                        (typeof(IHostedService).IsAssignableFrom(t) ||
                         t.GetCustomAttribute<HostedServiceAttribute>() != null));

        foreach (var serviceType in hostedServices)
        {
            var attr = serviceType.GetCustomAttribute<HostedServiceAttribute>();
            services.AddHostedService(sp =>
                (IHostedService)ActivatorUtilities.CreateInstance(sp, serviceType));

            // Optionally handle startImmediately flag
            if (attr?.RunImmediately == true)
            {
                services.Configure<HostOptions>(opts =>
                    opts.ServicesStartConcurrently = true);
            }
        }
    }
}