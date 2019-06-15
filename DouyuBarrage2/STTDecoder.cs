using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
namespace DouyuBarrage3
{
    class STTDecoder
    {
        private static string[] readTestData()
        {
            string[] datas = new string[40];
            try
            {
                // 创建一                 个 StreamReader 的实例来读取文件 
                // using 语句也能关闭 StreamReader
                using (StreamReader sr = new StreamReader("C:/Users/Admin/Desktop/File/barrAge3.txt"))
                {
                    string line;
                    int counter = 0;

                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {
                        //Console.WriteLine(line);
                        datas[counter++] = line;
                    }
                    //这里存在的问题是预先请求的string数组,可能会比实际读取的string个数多,造成string的length长度不对
                }

            }
            catch (Exception e)
            {
                // 向用户显示出错消息
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return datas;

        }
        private string data;
        //Store k/v pairs
        Dictionary<string, string> dic = new Dictionary<string, string>();
        //arraylist
        Dictionary<string, string[]> dicarray = new Dictionary<string, string[]>();
        //Store all the k/v pairs which contains k & "@=" & v
        string[] pairs;
        public STTDecoder()
        {
            this.data = "";
        }

        public STTDecoder(string data)
        {
            this.data = data;
        }

        public void process()
        {
            int counter = 0;

            //string[] datas1 = readTestData();
            //int length = datas1.Length;
            int length = 1;
            //Console.WriteLine("Data Length:" + length);
            for (int i = 0; i < length; i++)
            {
                this.dic.Clear();
                this.dicarray.Clear();
                //this.data = datas1[i];
                //this.data = this.data.Replace()
                pairs = data.Split('/');

                //Console.Write("*****" + ++counter + "*****\n");
                int len = pairs.Length;
                //foreach (string k in pairs)
                for (int j = 0; j < len - 1; j++)
                {
                    string k = pairs[j];
                    //If contains "@=" means that it is a pair
                    if (k.Contains("@=") && k.Trim() != "")
                    {
                        //string[] temp = k.Split(new char[2] { '@', '=' }); +
                        /*
                        k = k.Replace("@A", "@");
                        k = k.Replace("@S", "/");*/

                        string[] temp = Regex.Split(k, "@=", RegexOptions.IgnoreCase);
                        /*
                        temp[1] = temp[1].Replace("@S", "/");
                        temp[1] = temp[1].Replace("@A", "@");*/
                        //Before adding k&v to the dictionary,you should check the type of value(string/array) by checking if the value contains '/'
                        if (temp[1].Contains('/'))//array type
                        {

                            string[] temparray = temp[1].Split('/');
                            int l = temparray.Length;
                            if (temparray[l - 1] == "")
                            {
                                ArrayList al = new ArrayList(temparray);
                                al.RemoveAt(l - 1);
                                temparray = (string[])al.ToArray(typeof(string));
                            }
                            temp[0] = temp[0].Replace("@A", "@");
                            temp[0] = temp[0].Replace("@S", "/");

                            //temp[1] = temp[1].Replace("@A", "@");
                            temp[1] = temp[1].Replace("@S", "/");

                            int c = 0;
                            while (temp[1].Contains("@A"))
                            {
                                c++;
                                temp[1] = temp[1].Replace("@A", "@");
                                temp[1] = temp[1].Replace("@S", "/");
                            }
                            //Console.WriteLine("Counter1:" + c);

                            dicarray.Add(temp[0], temparray);
                        }
                        else
                        {
                            if (!dic.Keys.Contains(temp[0]))
                            {
                                temp[0] = temp[0].Replace("@A", "@");
                                temp[0] = temp[0].Replace("@S", "/");

                                //temp[1] = temp[1].Replace("@A", "@");
                                temp[1] = temp[1].Replace("@S", "/");
                                int c = 0;
                                while (temp[1].Contains("@A"))
                                {
                                    c++;
                                    temp[1] = temp[1].Replace("@A", "@");
                                    temp[1] = temp[1].Replace("@S", "/");
                                }
                                //Console.WriteLine("Counter2:" + c);

                                //dicarray.Add(temp[0], temparray);


                                dic.Add(temp[0], temp[1]);
                            }

                        }

                        //dic.Add(temp[0], temp[1]);
                    }
                }
            }



        }
        //Get all the keys
        
        public Dictionary<string,string> getKeys()
        {
            return dic;
        }
        //Get all the values
        /*
        public string[] getValues()
        {
            return "test";
        }*/
        //Get the value by key
        public string getKey(string key)
        {
            return dic[key];
        }
        //Get the array value

        public string[] getArrayValue(string key)
        {
            return dicarray[key];
        }

    }
}
