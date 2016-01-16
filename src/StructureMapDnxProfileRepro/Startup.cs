using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using StructureMap;

namespace StructureMapDnxProfileRepro
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var container = new Container();

            // Here we populate the container using the service collection.
            // This will register all services from the collection
            // into the container with the appropriate lifetime.
            container.Populate(services);

            container.Configure(cfg =>
            {
                cfg.For<IServiceScopeFactory>().ContainerScoped().Use<CustomStructureMapServiceScopeFactory>();
                cfg.For<ISessionFactory>().Use<DefaultSessionFactory>();

                cfg.Profile("TestProfile", _ =>
                {
                    _.For<ISessionFactory>().Use<OtherSessionFactory>();
                });
            });

            // Make sure we return an IServiceProvider, 
            // this makes DNX use the StructureMap container.
            return container.GetInstance<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseIISPlatformHandler();

            app.Run(async (context) =>
            {
                var sessionFactory = context.RequestServices.GetService<ISessionFactory>();
                await context.Response.WriteAsync($"Id: {sessionFactory.Id}");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }

    public interface ISessionFactory
    {
        Guid Id { get; }
    }

    public class DefaultSessionFactory : ISessionFactory
    {
        public Guid Id { get; set; }

        public DefaultSessionFactory()
        {
            Id = Guid.NewGuid();
        }

    }

    public class OtherSessionFactory : ISessionFactory
    {
        public Guid Id { get; set; }

        public OtherSessionFactory()
        {
            Id = Guid.Empty;
        }

    }

    internal sealed class CustomStructureMapServiceScopeFactory : IServiceScopeFactory
    {
        public CustomStructureMapServiceScopeFactory(IContainer container)
        {
            Container = container;
        }

        private IContainer Container { get; }

        public IServiceScope CreateScope()
        {
            var profile = Container.GetProfile("TestProfile");
            return new StructureMapServiceScope(profile.GetNestedContainer());
        }

        private class StructureMapServiceScope : IServiceScope
        {
            public StructureMapServiceScope(IContainer container)
            {
                Container = container;
                ServiceProvider = container.GetInstance<IServiceProvider>();
            }

            private IContainer Container { get; }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose() => Container.Dispose();
        }
    }
}
