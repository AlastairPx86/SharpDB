﻿/*
MIT License

Copyright (c) 2018 Jacob Paul

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SharpDB
{
    public class DB
	{

        private string dbPath;
        private string db;
        private string dbFolder;
        private string currentDbPath;
		private string slash;

        public DB(string path)
        {
			if (IsLinux) slash = "/";
			else slash = @"\";           
			dbPath = path;     
			dbFolder = dbPath + slash + "db";
        }

        public void CreateDatabase(string name)
        {
            //Check if 'db' directory exists, if not: create one
            if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);
            //Check if database exists so we don't overwrite an existing
            if (Directory.Exists(dbFolder + slash + name)) throw new Exception("Database already exists");
            else
            {
                //Create a new folder for the database
                Directory.CreateDirectory(dbFolder + slash + name);
                File.WriteAllLines(dbFolder + slash + name + slash + "properties.json", new string[] { "{ name:'" + name +  "',date:'" + DateTime.Now + "',tables:'0'}" });
            }
        }
        public void EnterDatabase(string dbName)
        {
            if (Directory.Exists(dbFolder + slash + dbName))
            {
                db = dbName;
                currentDbPath = dbFolder + slash + db;
            }
            else throw new Exception("Database does not exist");
        }
        public void CreateTable(string tbName, string tbIdQuery)
        {
            if (!string.IsNullOrEmpty(db))
            {
                if (!File.Exists(currentDbPath + slash + tbName + ".sdb"))
                {
					string[] tbId = tbIdQuery.Split(';');

                    string[] conf = { "<name>" + tbName + "</name><culumnLength>" + tbId.Length + "</culumnLength>" };
                    for(int i = 0; i < tbId.Length; i++)
                    {
                        conf[0] += "<tb" + i + ">" + tbId[i] + "</tb" + i + ">";
                    }
                    
                    File.WriteAllLines(currentDbPath + slash + tbName + ".sdb", conf);
                    
					string[] prop = File.ReadAllLines(currentDbPath + slash + "properties.json");
					dynamic propJson = JsonConvert.DeserializeObject(prop[0]);

					string tabNum = propJson.tables;
					propJson.tables = Int32.Parse(tabNum) + 1;
					prop[0] = JsonConvert.SerializeObject(propJson);

					File.WriteAllLines(currentDbPath + slash + "properties.json", prop);
                }
                else throw new Exception("Table already exists");
            }
            else throw new Exception("No database given (have you run EnterDatabase?)");
        }
        public void Insert(string tbName, string tbInfoQuery)
        {
            if (!string.IsNullOrEmpty(db))
            {
                if (File.Exists(currentDbPath + slash + tbName + ".sdb"))
                {
					string[] tbInfo = tbInfoQuery.Split(';');

                    string[] tb = File.ReadAllLines(currentDbPath + slash + tbName + ".sdb");
                    int tbLength = Int32.Parse(ExtractString(tb[0], "culumnLength"));
                    if (tbLength != tbInfo.Length) throw new Exception("Please enter the correct amount of values");
                    else
                    {
                        var _lines = new List<string>(tb);
                        string lineout = "";
                        for(int i = 0; i < tbLength; i++)
                        {
                            lineout += "<" + ExtractString(tb[0], "tb" + i) + ">" + tbInfo[i] + "</" + ExtractString(tb[0], "tb" + i) + ">";
                        }
                        _lines.Add(lineout);
                        var linesArr = _lines.ToArray();
                        //Array.Sort(linesArr);

                        File.WriteAllLines(currentDbPath + slash + tbName + ".sdb", linesArr);
                        
                    }
                }
                else throw new Exception("Table does not exist!");
            }
            else throw new Exception("No database given (have you run EnterDatabase?)");
        }
        public string[] Get(string toGetQuery, string tbName, string argQuery)
        {
			var argNameList = new List<string>();
			var argInfoList = new List<string>();

			string[] argArr = argQuery.Split(';');
			if (!string.IsNullOrEmpty(argQuery))
			{
				for (int i = 0; i < argArr.Length; i++)
				{
					string[] splitter = argArr[i].Split('=');
					argNameList.Add(splitter[0]);
					argInfoList.Add(splitter[1]);
				}
			}

			string[] argName = argNameList.ToArray();
			string[] arg = argInfoList.ToArray();
			string[] toGet = toGetQuery.Split(';');

            var list = new List<string>();
            string[] tb = File.ReadAllLines(currentDbPath + slash + tbName + ".sdb");
            int columnLength = tb.Length - 1;

			if(toGet[0] == "[ALL]"){
				var allList = new List<string>();
				for (int c = 0; c <= columnLength; c++){
					allList.Add(ExtractString(tb[0], "tb" + c.ToString()));
				}
				toGet = allList.ToArray();
			}

            int su = 0;
            for (int i = 1; i <= columnLength; i++)
            {
                for(int t = 0; t < argName.Length; t++)
                {
                    string wocon = arg[t].Replace("[CONTAINS]", "");
                    if (ExtractString(tb[i], argName[t]) == arg[t] || (arg[t].Contains("[CONTAINS]") && ExtractString(tb[i], argName[t]).Contains(wocon))) su++;
                    else break;
                    
                }
				if (su == argName.Length)
				{
					for (int d = 0; d < toGet.Length; d++)
					{
						list.Add(ExtractString(tb[i], toGet[d]));
					}
					
					
				}
                su = 0;

            }
			return list.ToArray();
        }
        public void DeleteDatabase(string dbName)
        {
            if (!Directory.Exists(dbFolder + slash + dbName)) throw new Exception("Database does not exist");
            else
            {
                DirectoryInfo di = new DirectoryInfo(dbFolder + slash + dbName);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
                Directory.Delete(dbFolder + slash + dbName);

            }
        }
        public bool DatabaseExists(string dbName)
        {
            if (Directory.Exists(dbFolder + slash + dbName)) return true;
            else return false;
        }
        public bool TableExists(string dbName, string tableName)
        {
            if (Directory.Exists(dbFolder + slash + dbName))
            {
                if (File.Exists(dbFolder + slash + dbName + slash + tableName + ".sdb")) return true;
                else return false;
            }
            else return false;
        }
		public int CountTables(string dbName){
			if (!DatabaseExists(dbName)) throw new Exception("Database does not exist");
			string[] tb = File.ReadAllLines(dbFolder + slash + dbName + slash + "properties.json");
			dynamic json = JsonConvert.DeserializeObject(tb[0]);
			string tbCount = json.tables;
			return Int32.Parse(tbCount);
            
		}
		public int TableLength(string dbName, string tbName){
			if (!TableExists(dbName, tbName)) throw new Exception("Table or database does not exist");
			string[] tb = File.ReadAllLines(dbFolder + slash + dbName + slash + tbName + ".sdb");
			return tb.Length - 1;
		}
        private string ExtractString(string s, string tag)
        {
            // You should check for errors in real-world code, omitted for brevity
            var startTag = "<" + tag + ">";
            int startIndex = s.IndexOf(startTag) + startTag.Length;
            int endIndex = s.IndexOf("</" + tag + ">", startIndex);
            return s.Substring(startIndex, endIndex - startIndex);
        }
		private bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
    }
}
