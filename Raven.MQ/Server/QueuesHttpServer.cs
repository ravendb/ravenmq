using System;
using System.Text.RegularExpressions;
using Raven.Http;
using Raven.Http.Abstractions;

namespace RavenMQ.Server
{
    public class QueuesHttpServer : HttpServer
    {
        public QueuesHttpServer(IRaveHttpnConfiguration configuration, IResourceStore resourceStore) : base(configuration, resourceStore)
        {
        }

        private static readonly Regex databaseQuery = new Regex("^/databases/([^/]+)(?=/?)");

        public override Regex TenantsQuery
        {
            get { return databaseQuery; }
        }


        protected override bool TryHandleException(IHttpContext ctx, Exception exception)
        {
            return false;
        }

        protected override bool TryGetOrCreateResourceStore(string name, out IResourceStore database)
        {
            database = null;
            return false;
        }
    }
}