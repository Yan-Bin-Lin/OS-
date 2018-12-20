using System;
using System.Threading;
using System.Collections.Generic;

namespace program
{
    class RW
    {
        //global variable
        public static class global
        {
            public enum StatusType { wait, run, done };
            public static int turn_writer;// how much time a turn of writer
            public static int turn_reader;// how much time a turn of reader
            public static int done_writer;//how many writer is done
            public static int done_reader;//how many reader is done
            public static bool pause;//"only" system can make all thread pause
            public static bool turn;//0 = reader's turn, 1 = writer's turn
            public static int next_writer;//the next writer allow to write
            public static int max_int;//the max number of int 
            public static Mutex mutexR = new Mutex();//use mutex to change wait count
            public static Mutex mutexW = new Mutex();//use mutex to change wait count
            public static int WorkTimeR;//the time reader stay in critical section
            public static int WorkTimeW;//the time writer stay in critical section
            public static int unfinishedR;//how many reader are not finish
            public static int unfinishedW;//how many writer are not finish
        }

        //the basic attribute and method of reader and writer
        public class basic
        {
            //mointer can set only in constructor
            //variable need to be looked
            public int work_time,/*the time stay in certical section*/
                       ID, status;
            public string word;//local for read or write

            //method not for moniter
            protected static ReaderWriterLockSlim RWLock = new ReaderWriterLockSlim();//RWLock
            protected static string buffer;// lock  

            //constructor
            public basic(int work_time, int ID)
            {
                this.work_time = work_time;
                this.ID = ID;
                this.status = (int)global.StatusType.wait;
            }

            protected void check_pause()
            {
                while (global.pause) { }
            }

        }

        public class reader : basic
        {


            //constructor
            public reader(int work_time, int ID)
                : base(work_time, ID) { }

            public void start()
            {
                Thread thread = new Thread(read);
                thread.IsBackground = true;
                thread.Start();
            }

            //reader's job
            private void read()
            {
                //only reader's turn && system is not pause can go next step
                while (global.turn == true || global.pause) { }
                //get lock and read
                try_read();
            }

            //try to read
            private void try_read()
            {
                RWLock.EnterReadLock();

                check_pause();//system call pause;
                status = (int)global.StatusType.run;

                critical();

                RWLock.ExitReadLock();

                check_pause();
                //add to done
                global.mutexR.WaitOne();
                global.done_reader++;
                global.mutexR.ReleaseMutex();

                status = (int)global.StatusType.done;
                Console.WriteLine(ID.ToString() + " finish the read_job");
            }

            //critical section
            private void critical()
            {
                Console.WriteLine(ID.ToString() + " start to read, we can read together");
                Thread.Sleep(work_time);

                if (buffer == null) word = "No one write here";
                else word = String.Copy(buffer);
                check_pause();//system call pause


                Console.WriteLine(ID.ToString() + " finsh reading, the string is --- " + word + " ---");
            }

        }

        public class writer : basic
        {


            //constructor
            public writer(int work_time, int ID, string word)
                : base(work_time, ID)
            {
                this.word = word;
            }

            public void start()
            {
                Thread thread = new Thread(write);
                thread.IsBackground = true;
                thread.Start();
            }

            //writer's job
            private void write()
            {
                //only writer's turn && system is not pause && he is the next writer can go next step
                while (global.turn == false || global.pause || ID != global.next_writer) { }
                //get lock and write
                try_write();
            }

            //try to write
            private void try_write()
            {
                RWLock.EnterWriteLock();

                check_pause();//system call pause;
                status = (int)global.StatusType.run;

                critical();

                RWLock.ExitWriteLock();

                check_pause();//system call pause

                global.mutexW.WaitOne();

                global.done_writer++;
                global.next_writer = CheckAndAdd(ID);

                global.mutexW.ReleaseMutex();

                status = (int)global.StatusType.done;
                Console.WriteLine(ID.ToString() + " finish the write_job");
            }

            //critical section
            private void critical()
            {
                Console.WriteLine(ID.ToString() + " start to write, only he can write");
                Thread.Sleep(work_time);
                check_pause();//system call pause

                buffer = String.Copy(word);
                Console.WriteLine(ID.ToString() + " write ' " + word + " '");
                Console.WriteLine("Now, the buffer is ' " + buffer + " '");

                Console.WriteLine(ID.ToString() + " finsh writing");
            }

        }


        public class Readers
        {
            //not for user
            private int IDcount, start_index, unfinished_number;

            //variable for user
            public int work_time;
            public List<reader> readers;

            //constructor
            public Readers()
            {
                this.IDcount = 0;
                this.start_index = 0;
                this.unfinished_number = 0;
                this.readers = new List<reader>();
                this.work_time = global.WorkTimeR;
            }

            //add one reader
            public void add()
            {
                readers.Add(new reader(work_time, IDcount));
                IDcount = CheckAndAdd(IDcount);
            }

            //start to run
            public void start()
            {
                //total unfinished_number
                if (start_index < IDcount)
                {
                    unfinished_number += IDcount - start_index;
                    for (int i = start_index; i < IDcount; i++)
                    {
                        //Console.WriteLine(i.ToString() + " is start");
                        readers[i].start();
                    }
                    start_index = IDcount;
                }
                else if (start_index > IDcount)
                {
                    unfinished_number += global.max_int - start_index + 1 + IDcount;
                    for (int i = start_index; i <= global.max_int; i++)
                    {
                        readers[i].start();
                    }
                    for (int i = 0; i < IDcount; i++)
                    {
                        readers[i].start();
                    }
                    start_index = IDcount;
                }
                else
                {
                    return;
                }
            }

            public int get_unfinished_number()
            {
                if (readers.Count == 0) return 0;
                //call pause to get mutex easier;
                global.pause = true;
                global.mutexR.WaitOne();

                int number = unfinished_number - global.done_reader;
                global.done_reader = 0;

                global.mutexR.ReleaseMutex();
                global.pause = false;
                unfinished_number = number;
                return number;
            }

            public void clear()
            {
                readers.Clear();
            }

            public void restart()
            {
                IDcount = 0;
                start_index = 0;
                unfinished_number = 0;
                global.done_reader = 0;
            }

        }

        public class Writers
        {
            //not for user
            private int IDcount, start_index, unfinished_number;

            //variable for user
            public int work_time;
            public string word;
            public List<writer> writers;

            //constructor
            public Writers()
            {
                this.IDcount = 0;
                this.start_index = 0;
                this.unfinished_number = 0;
                this.writers = new List<writer>();
                this.work_time = global.WorkTimeW;
            }

            //add one writer
            public void add()
            {
                word = IDcount.ToString() + " write here";
                writers.Add(new writer(work_time, IDcount, word));
                IDcount = CheckAndAdd(IDcount);
            }

            //start to run
            public void start()
            {
                //total unfinished_number
                if (start_index < IDcount)
                {
                    unfinished_number += IDcount - start_index;
                    for (int i = start_index; i < IDcount; i++)
                    {
                        //Console.WriteLine("writer[" + i.ToString() + "] is start");
                        writers[i].start();
                    }
                    start_index = IDcount;
                    //Console.WriteLine("the count of writer to start is " + IDcount.ToString());
                }
                else if (start_index > IDcount)
                {
                    unfinished_number += global.max_int - start_index + 1 + IDcount;
                    for (int i = start_index; i <= global.max_int; i++)
                    {
                        writers[i].start();
                    }
                    for (int i = 0; i < IDcount; i++)
                    {
                        writers[i].start();
                    }
                    start_index = IDcount;
                }
                else
                {
                    return;
                }
            }

            public int get_unfinished_number()
            {
                if (writers.Count == 0) return 0;
                //Console.WriteLine("the count of writer unfinished is " + unfinished_number.ToString());
                //call pause to get mutex easier;
                global.pause = true;
                global.mutexW.WaitOne();

                int number = unfinished_number - global.done_writer;
                //Console.WriteLine("the count of writer done is " + global.done_writer.ToString());
                global.done_writer = 0;

                global.mutexW.ReleaseMutex();
                global.pause = false;
                unfinished_number = number;
                return number;
            }

            public void clear()
            {
                writers.Clear();
            }

            public void restart()
            {
                IDcount = 0;
                start_index = 0;
                unfinished_number = 0;
                global.done_writer = 0;
                global.next_writer = 0;
            }

        }

        public class keybelong
        {
            //0=reader , 1=writer
            public bool belong;
            private int base_work_time = 100, buffer = 100;
            private int writer_count_max = 10, writer_do = 0, writer_do_max = 10;

            public void fix_rt(int value)
            {
                if (value > 0) global.WorkTimeR = value;
            }

            public void fix_wt(int value)
            {
                if (value > 0) global.WorkTimeW = value;
            }

            public void fix_work_time(int basic)
            {
                if (basic > 0) base_work_time = basic;
            }

            public void fix_writer_count_max(int value)
            {
                if (value > 0) writer_count_max = value;
            }

            public int gettime(int reader_count, int writer_count)
            {

                int re;

                if (writer_do >= writer_do_max)
                {
                    belong = false;
                    re = base_work_time + buffer;
                    if (re < global.WorkTimeR) re = global.WorkTimeR;
                    writer_do = 0;
                }

                else if (writer_count >= writer_count_max || writer_count > reader_count)
                {
                    belong = true;
                    re = (writer_count * base_work_time) + buffer;
                    if (re < global.WorkTimeW) re = global.WorkTimeW;
                    writer_do++;
                }

                else
                {
                    belong = false;
                    re = base_work_time + buffer;
                    if (re < global.WorkTimeR) re = global.WorkTimeR;
                    writer_do = 0;
                }

                return re;
            }
        }

        public static void change()
        {
            keybelong key = new keybelong();
            while (true)
            {
                /*
                //writer's time
                Console.WriteLine("writer's time");
                global.turn = true;

                Console.WriteLine("----------------------------------------------------------------------------- the unfinished numer of writer is " + global.unfinishedW.ToString());
                Console.WriteLine("----------------------------------------------------------------------------- the unfinished numer of reader is " + global.unfinishedR.ToString());

                global.turn_writer = (int)key.gettime(global.unfinishedR, global.unfinishedW);
                Thread.Sleep(global.turn_writer);
                //reader's time
                Console.WriteLine("reader's time");
                global.turn = false;

                Console.WriteLine("----------------------------------------------------------------------------- the unfinished numer of writer is " + global.unfinishedW.ToString());
                Console.WriteLine("----------------------------------------------------------------------------- the unfinished numer of reader is " + global.unfinishedR.ToString());

                global.turn_reader = (int)key.gettime(global.unfinishedR, global.unfinishedW);
                Thread.Sleep(global.turn_reader);
                */

                global.turn_writer = global.turn_reader = key.gettime(global.unfinishedR, global.unfinishedW);
                global.turn = key.belong;
                if (global.turn == true)
                {
                    Thread.Sleep(global.turn_writer);
                }
                else
                {
                    Thread.Sleep(global.turn_reader);
                }

            }
        }

        //circle
        public static int CheckAndAdd(int num)
        {
            return num == global.max_int ? 0 : num + 1;
        }

        public class poisson
        {

            Random ran = new Random();
            //VAL會與柏松分布的FUNC所計算出來的值比較大小
            double val;

            //給予VAL值
            public poisson()
            {
                getnext();
            }

            //讓VAL得到一個(>0.0) && (<1.0) 的值
            private void getnext()
            {
                for (val = ran.NextDouble(); val == 0;) val = ran.NextDouble();
            }

            //計算柏松分布在k=times時的機率
            private double pro_of_poi(double lamda, double times)
            {

                double pro = Math.Pow(Math.E, ((-1) * lamda));

                for (int i = 0; i < times; i++)
                {
                    pro /= (i + 1);
                    pro *= lamda;
                }
                return pro;
            }

            //回傳在k=times時，隨機變數 >= P(x=times)
            public bool is_new_product(double lamda, double times)
            {
                getnext();

                return val >= pro_of_poi(lamda, times);
            }
        }

        public static void ar()
        {
            DateTime time_start = new DateTime(DateTime.MinValue.Ticks);
            Readers r = new Readers();
            poisson rpo = new poisson();
            double count = 0;
            Console.WriteLine("readers call start()");
            while (true)
            {
                Thread.Sleep(17);
                if (rpo.is_new_product(DateTime.Now.ToOADate(), count))
                {
                    r.add();
                    count++;
                }
                global.unfinishedR = r.get_unfinished_number();

                r.start();
            }
        }

        public static void aw()
        {
            DateTime time_start = new DateTime(DateTime.MinValue.Ticks);
            Writers w = new Writers();
            poisson wpo = new poisson();
            double count = 0;
            Console.WriteLine("writers call start()");
            while (true)
            {
                Thread.Sleep(17);
                if (wpo.is_new_product(DateTime.Now.ToOADate(), count))
                {
                    w.add();
                    count++;
                }
                global.unfinishedW = w.get_unfinished_number();

                w.start();
            }
        }

        static void Main()
        {
            //innitial
            global.pause = false;
            global.done_reader = 0;
            global.done_writer = 0;
            global.next_writer = 0;
            global.turn_reader = 1000;
            global.turn_writer = 20000;
            global.max_int = int.MaxValue;
            global.WorkTimeR = 100;
            global.WorkTimeW = 100;
            global.unfinishedW = 0;
            global.unfinishedR = 0;

            Thread _switch = new Thread(change);
            _switch.Start();

            Thread rs = new Thread(ar);
            rs.Start();

            Thread ws = new Thread(aw);
            ws.Start();

            //Console.WriteLine("----------------------------------------------------------------------------- the unfinished numer of reader is " + r.get_unfinished_number().ToString());
            //Console.WriteLine("----------------------------------------------------------------------------- the unfinished numer of writer is " + w.get_unfinished_number().ToString());
        }
    }
}
