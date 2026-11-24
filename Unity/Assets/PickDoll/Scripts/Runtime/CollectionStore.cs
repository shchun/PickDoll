using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PickDoll
{
    // Guest progress only. A completed receipt and its species are committed together.
    public sealed class CollectionStore
    {
        [Serializable] public sealed class Receipt { public string ticket, species; }
        [Serializable] private sealed class Save { public int version; public List<Receipt> receipts=new List<Receipt>(); }
        private Save data=new Save {version=1};
        private readonly HashSet<string> tickets=new HashSet<string>();
        public string Path { get; }
        public string Error { get; private set; }
        public bool RecoveredBackup { get; private set; }
        public int Total => data.receipts.Count;
        public int CountFor(string species)=>data.receipts.Count(r=>r.species==species);
        private static readonly string testDirectory="PickDoll-Week07-"+Guid.NewGuid().ToString("N");
        public static string DefaultPath {
            get {
                var args=Environment.GetCommandLineArgs();
                if(args.Contains("--pickdoll-week07-test")||args.Contains("--pickdoll-week07-ui")||args.Contains("--pickdoll-week08-test")||args.Contains("--pickdoll-week08-ui"))
                    return System.IO.Path.Combine(System.IO.Path.GetTempPath(),testDirectory,"collection.json");
                return System.IO.Path.Combine(Application.persistentDataPath,"collection-v1.json");
            }
        }
        public CollectionStore(string path) {
            Path=path;
            if(File.Exists(path)||File.Exists(path+".bak")) {
                if(!TryRead(path,out data)) {
                    if(TryRead(path+".bak",out data))RecoveredBackup=true;
                    else {data=new Save {version=1};Error="Cannot read collection save. Existing files were preserved.";}
                }
            }
            foreach(var r in data.receipts)tickets.Add(r.ticket);
        }
        private static bool TryRead(string path,out Save save) {
            save=null;
            try {
                var candidate=JsonUtility.FromJson<Save>(File.ReadAllText(path));
                if(candidate==null||candidate.version!=1||candidate.receipts==null)return false;
                var unique=new HashSet<string>();
                foreach(var r in candidate.receipts)if(r==null||string.IsNullOrEmpty(r.ticket)||string.IsNullOrEmpty(r.species)||!unique.Add(r.ticket))return false;
                save=candidate;return true;
            }catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException){return false;}
        }
        public bool Commit(string ticket,string species,out bool added) {
            added=false;if(tickets.Contains(ticket))return true;
            if(Error!=null||string.IsNullOrEmpty(ticket)||string.IsNullOrEmpty(species))return false;
            var next=new Save {version=1,receipts=new List<Receipt>(data.receipts)};
            next.receipts.Add(new Receipt {ticket=ticket,species=species});
            try {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                var bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(next,true));
                using(var stream=new FileStream(Path+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                // Only replace the backup with a readable primary; recovery must survive another crash.
                if(TryRead(Path,out _))File.Copy(Path,Path+".bak",true);
                if(File.Exists(Path))File.Replace(Path+".tmp",Path,null);else File.Move(Path+".tmp",Path);
                data=next;tickets.Add(ticket);added=true;
                // First save has a backup too. Failure here does not undo the committed primary.
                try {if(!File.Exists(Path+".bak"))File.Copy(Path,Path+".bak");}catch(IOException){}
                return true;
            }catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Error="Collection save failed: "+e.Message;Debug.LogError(Error);return false;}
        }
    }
}
