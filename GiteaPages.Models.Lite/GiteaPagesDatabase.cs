using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace GiteaPages.Models.Lite {
    public class GiteaPagesDatabase : LiteDatabase {
        public GiteaPagesDatabase(string path) : base(path) { }


        public RepositoryRecord Get(string user, string repository) {
            user = user?.ToLower();
            repository = repository?.ToLower();

            var coll = GetCollection<RepositoryRecord>();

            return coll.FindOne(x => x.User == user && x.Name == repository);
        }

        public void CreateOrUpdateRecord(RepositoryRecord record) {
            var coll = GetCollection<RepositoryRecord>();

            var instance = Get(record.User, record.Name);

            if (instance == null) {
                instance = record;
                coll.Insert(instance);
                return;
            }

            instance.LastMasterCommitId = record.LastMasterCommitId;
        }
    }
}
