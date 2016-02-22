using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis;

/* note - todo: clean up hard coded results to text file implementation */
/* you can comment this out / refactor */

/* lucene version is lucene_29 */

namespace sweeTLucene
{
    public abstract class LuceneIndexAndSearch
    {
        public abstract int numHits { get; set; }
        public abstract string path { get; set; }
        public abstract string rslt { get; set; }
        public abstract string LucGo(bool index, bool ramindex, bool search, bool lookinside, string path, string queryStr, List<string> fileExtensionsToIndex);
    }
    public class SweetLuceneWrapper : LuceneIndexAndSearch
    {
        public override int numHits { get; set; }
        public override string path { get; set; }
        public override string rslt { get; set; }
        UseLuceneIndexAndSearch worker = null;
        UseLuceneRAMIndexAndSearch RAMworker = null; // ram indexed lucene worker
        public override string LucGo(bool index, bool ramindex, bool search, bool lookinside, string path, string queryStr, List<string> fileExtensionsToIndex)
        {
            if (!ramindex)
            {
                worker = new UseLuceneIndexAndSearch();
                if ((index) && (search))
                {
                    worker.index(path, fileExtensionsToIndex, lookinside);
                    rslt = worker.search(path, queryStr);
                }
                if ((index) && (!search))
                {
                    worker.index(path, fileExtensionsToIndex, lookinside);
                }
                if ((!index) && (search))
                {
                    rslt = worker.search(path, queryStr);
                }
            }
            if (ramindex)
            {
                RAMworker = new UseLuceneRAMIndexAndSearch();
                if ((index) && (search))
                {
                    RAMworker.index(path, fileExtensionsToIndex, lookinside);
                    rslt = RAMworker.search(path, queryStr);
                }
                if ((index) && (!search))
                {
                    RAMworker.index(path, fileExtensionsToIndex, lookinside);
                }
                if ((!index) && (search))
                {
                    rslt = RAMworker.search(path, queryStr);
                }
            }
            return rslt;
        }
    }
    public class UseLuceneIndexAndSearch
    {
        List<string> dotIndexFilestoLuceneIndex = new List<string>();
        Lucene.Net.Documents.Document doc;
        Analyzer std;
        string strIndexDir;
        public void ProcessDirectory(string targetDirectory, List<string> fileExts)
        {
            // Process the list of files found in the directory. 
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                foreach (string ext in fileExts)
                {
                    if (fileName.Contains(ext))
                    {
                        dotIndexFilestoLuceneIndex.Add(fileName);
                    }
                }
            }
            // Recurse into subdirectories of this directory. 
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory, fileExts);
            }
        }
        public void index(string path, List<string> fileExts, bool lookinside)
        {
            try
            {
                strIndexDir = path;
                string fileContents = "";
                /*
                 * get paths to all .index files and stuff into searchable index doc
                 */
                ProcessDirectory(path, fileExts); // stuff .index filenames into a list for ingestion as fields in lucene doc (below) 
                /*
                 * now I want to write all indexable file contents/txt to index
                 */
                Lucene.Net.Store.Directory indexDir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(strIndexDir));
                std = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29); //Version parameter is used for backward compatibility. Stop words can also be passed to avoid indexing certain words
                IndexWriter idxw = new IndexWriter(indexDir, std, true, IndexWriter.MaxFieldLength.UNLIMITED); //Create an Index writer object.
                doc = new Lucene.Net.Documents.Document();
                foreach (string f in dotIndexFilestoLuceneIndex)
                {
                    using (FileStream fs = File.Open(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (BufferedStream bs = new BufferedStream(fs))
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            fileContents += line;

                        }
                        doc = new Lucene.Net.Documents.Document();
                        if (lookinside) // search within files?
                        {
                            doc.Add(new Lucene.Net.Documents.Field("fileContents", fileContents, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                        }
                        doc.Add(new Lucene.Net.Documents.Field("path", f, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                        try
                        {
                            idxw.AddDocument(doc);
                        }
                        catch { }
                    }
                }
                idxw.Optimize();
                idxw.Commit();
                idxw.Dispose();
            }
            catch { }
        }
        public string search(string path, string queryStr)
        {
            std = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_29, "fileContents", std);
            queryStr = QueryParser.Escape(queryStr);
            Lucene.Net.Search.Query qry = parser.Parse(queryStr);
            strIndexDir = path;
            Lucene.Net.Store.Directory directory = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(strIndexDir)); //Provide the directory where index is stored
            Lucene.Net.Search.Searcher srchr = new Lucene.Net.Search.IndexSearcher(Lucene.Net.Index.IndexReader.Open(directory, true));//true opens the index in read only mode
            TopDocs topDocs = srchr.Search(qry, 50);
            int results = topDocs.ScoreDocs.Length;
            if (results != 0)
            {
                for (int i = 0; i < 50; i++) // use results for all
                {
                    ScoreDoc scoreDoc = topDocs.ScoreDocs[i];
                    int docId = scoreDoc.Doc;
                    doc = srchr.Doc(docId);
                    string rslt = doc.Get("path");
                }
            }
            srchr.Dispose();
            directory.Dispose();
            return doc.Get("path");
        }
        public UseLuceneIndexAndSearch()
        {
        }
    }
    public class UseLuceneRAMIndexAndSearch
    {   /* not impl. yet 2-22-2016 */
        List<string> dotIndexFilestoLuceneIndex = new List<string>();
        Lucene.Net.Documents.Document doc;
        Analyzer std;
        string strIndexDir;
        public void ProcessDirectory(string targetDirectory, List<string> fileExts)
        {
            // Process the list of files found in the directory. 
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                foreach (string ext in fileExts)
                {
                    if (fileName.Contains(ext))
                    {
                        dotIndexFilestoLuceneIndex.Add(fileName);
                    }
                }
            }
            // Recurse into subdirectories of this directory. 
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory, fileExts);
            }
        }
        public void index(string path, List<string> fileExts, bool lookinside)
        {
            try
            {
                strIndexDir = path;
                string fileContents = "";
                /*
                 * get paths to all .index files and stuff into searchable index doc
                 */
                ProcessDirectory(path, fileExts); // stuff .index filenames into a list for ingestion as fields in lucene doc (below) 
                /*
                 * now I want to write all indexable file contents/txt to RAM index
                 */
                Lucene.Net.Store.Directory indexDir = new Lucene.Net.Store.RAMDirectory();
                std = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29); //Version parameter is used for backward compatibility. Stop words can also be passed to avoid indexing certain words
                IndexWriter idxw = new IndexWriter(indexDir, std, true, IndexWriter.MaxFieldLength.UNLIMITED); //Create an Index writer object.
                doc = new Lucene.Net.Documents.Document();
                foreach (string f in dotIndexFilestoLuceneIndex)
                {
                    using (FileStream fs = File.Open(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (BufferedStream bs = new BufferedStream(fs))
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            fileContents += line;

                        }
                        doc = new Lucene.Net.Documents.Document();
                        if (lookinside) // search within files?
                        {
                            doc.Add(new Lucene.Net.Documents.Field("fileContents", fileContents, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                        }
                        doc.Add(new Lucene.Net.Documents.Field("path", f, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                        try
                        {
                            idxw.AddDocument(doc);
                        }
                        catch { }
                    }
                }
                idxw.Optimize();
                idxw.Commit();
                idxw.Dispose();
            }
            catch { }
        }
        public string search(string path, string queryStr)
        {
            std = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_29, "fileContents", std);
            queryStr = QueryParser.Escape(queryStr);
            Lucene.Net.Search.Query qry = parser.Parse(queryStr);
            strIndexDir = path;
            Lucene.Net.Store.Directory directory = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(strIndexDir)); //Provide the directory where index is stored
            Lucene.Net.Search.Searcher srchr = new Lucene.Net.Search.IndexSearcher(Lucene.Net.Index.IndexReader.Open(directory, true));//true opens the index in read only mode
            TopDocs topDocs = srchr.Search(qry, 50);
            int results = topDocs.ScoreDocs.Length;
            if (results != 0)
            {
                for (int i = 0; i < 50; i++) // use results for all
                {
                    ScoreDoc scoreDoc = topDocs.ScoreDocs[i];
                    int docId = scoreDoc.Doc;
                    doc = srchr.Doc(docId);
                    string rslt = doc.Get("path");
                }
            }
            srchr.Dispose();
            directory.Dispose();
            return doc.Get("path");
        }
        public UseLuceneRAMIndexAndSearch()
        {
            /* not impl. yet 2-22-2016 */
            //throw new System.Exception();
        }
    }
}

