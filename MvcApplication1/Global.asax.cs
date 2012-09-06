using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MvcApplication1.Controllers;
using Raven.Abstractions.Replication;
using Raven.Bundles.IndexReplication.Data;
using Raven.Bundles.Versioning.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;

namespace MvcApplication1
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        //One DocumentStore per app!
        public static IDocumentStore DocumentStore { get; private set; } 

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            InitializeDocumentStore();
        }

        private void InitializeDocumentStore()
        {
            DocumentStore = new DocumentStore {Url = "http://localhost:8080", DefaultDatabase = "BasicOps"};
            DocumentStore.Initialize();
            DocumentStore.AggressivelyCacheFor(TimeSpan.FromMinutes(5));
            DocumentStore.Conventions.FailoverBehavior = FailoverBehavior.AllowReadsFromSecondaries;
            IndexCreation.CreateIndexes(typeof(StudentsByCourse).Assembly, DocumentStore);

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ReplicationDocument
                    {
                        Destinations =
                            new List<ReplicationDestination>
                                {new ReplicationDestination {Database = "Slave", Url = "http://localhost:8080"},
                                new ReplicationDestination {Database = "Slave2", Url = "http://localhost:8080"}}
                    });
                session.Store(new VersioningConfiguration
                    {
                        Id = "Raven/Versioning/DefaultConfiguration",
                        Exclude = false,
                        MaxRevisions = 5
                    });
                session.Store(new IndexReplicationDestination
                    {
                        Id = "Raven/IndexReplication/ReportTest",
                        ColumnsMapping = {{"Name", "Name"}, {"Price", "Price"},},
                        ConnectionStringName = "SQLConnection",
                        PrimaryKeyColumnName = "Id",
                        TableName = "Test"
                    });
                session.SaveChanges();
            }
        }
    }
}