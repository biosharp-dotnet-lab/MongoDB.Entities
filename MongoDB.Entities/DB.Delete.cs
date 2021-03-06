﻿using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        private static async Task<DeleteResult> DeleteCascadingAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            // note: cancellation should not be enabled because multiple collections are involved 
            //       and premature cancellation could cause data inconsistencies.

            var tasks = new HashSet<Task>();
            var db = GetDatabase<T>();
            var options = new ListCollectionNamesOptions
            {
                Filter = "{$and:[{name:/~/},{name:/" + CollectionName<T>() + "/}]}"
            };

            foreach (var cName in await db.ListCollectionNames(options).ToListAsync())
            {
                tasks.Add(
                    session == null
                    ? db.GetCollection<JoinRecord>(cName).DeleteManyAsync(r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID))
                    : db.GetCollection<JoinRecord>(cName).DeleteManyAsync(session, r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID), null));
            }

            var delRes =
                    session == null
                    ? Collection<T>().DeleteManyAsync(x => IDs.Contains(x.ID))
                    : Collection<T>().DeleteManyAsync(session, x => IDs.Contains(x.ID), null);

            tasks.Add(delRes);

            if (typeof(T).BaseType == typeof(FileEntity))
            {
                tasks.Add(
                    session == null
                    ? db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(x => IDs.Contains(x.FileID))
                    : db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(session, x => IDs.Contains(x.FileID), null));
            }

            await Task.WhenAll(tasks);

            return await delRes;
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(ID, session));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(string ID, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(ID, session));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static Task<DeleteResult> DeleteAsync<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(new[] { ID }, session);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(string ID, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DeleteCascadingAsync<T>(new[] { ID }, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync(expression, session));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync(expression, session));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static async Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            var IDs = await Find<T, string>()
                            .Match(expression)
                            .Project(e => e.ID)
                            .ExecuteAsync();

            return await DeleteCascadingAsync<T>(IDs, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DeleteAsync(expression, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(IDs, session));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(IDs, session));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(IDs, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DeleteCascadingAsync<T>(IDs, session);
        }
    }
}
