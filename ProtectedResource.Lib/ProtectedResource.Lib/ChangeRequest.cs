using System;

namespace ProtectedResource.Lib
{
    public class ChangeRequest<T>
    {
        /// <summary>
        /// Requester provided token so that they can listen for when the request is finished regardless of outcome.
        /// </summary>
        public Guid RequestToken { get; set; }

        /// <summary>
        /// The incoming resource (Entity) that has been modified and needs to be persisted.
        /// </summary>
        public T ModifiedResource { get; set; }

        public string PatchJson { get; set; }

        /// <summary>
        /// Place holder. I am trying to figure out how to generically allow for a particular search (WHERE clause)
        /// for any object. Not sure if this will work out.
        /// </summary>
        public string PartitionKey { get; set; }
    }
}
