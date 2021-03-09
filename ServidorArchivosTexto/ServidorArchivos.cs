using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServidorArchivosTexto
{
    class ServidorArchivos
    {
        static readonly internal object l = new object();
        internal string leeArchivo(string nombreArchivo, int nLineas)
        {
            string lineas = "";
            int n = 0;

            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("EXAMEN") + "\\" + nombreArchivo))
                {
                    string l = sr.ReadLine();
                    while (l != null && n < nLineas)
                    {
                        lineas += l + "\r\n";
                        n++;
                        l = sr.ReadLine();
                    }
                }
            }
            catch (IOException)
            {
                return "<ERROR_IO>";
            }
            return lineas;
        }

        internal int leePuerto()
        {
            int puertoPorDefecto = 31416;
            int puertoArchivo;
            if(Int32.TryParse(leeArchivo("puerto.txt", 1), out puertoArchivo))
            {
                if(puertoArchivo >= 0 && puertoArchivo <= 65535)
                {
                    return puertoArchivo;
                }                 
            }
            return puertoPorDefecto;
        }

        internal void guardaPuerto(int numero)
        {
            try
            {
                using(StreamWriter sw = new StreamWriter(Environment.GetEnvironmentVariable("EXAMEN") + "\\puerto.txt"))
                {
                    sw.WriteLine(numero);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Error al guardar puerto "+e.Message);
            }
        }

        internal string listaArchivos()
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetEnvironmentVariable("EXAMEN"));
            FileInfo[] files = dir.GetFiles();
            string nameFiles = "";
            foreach (FileInfo file in files)
            {
                if(file.Extension == ".txt")
                {
                    nameFiles += file.Name + "\r\n";
                }
            }
            return nameFiles;
        }

        private Socket s;
        private bool connected = true;
        internal void iniciaServidorArchivos()
        {
            int port = leePuerto();
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, port);
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
            try
            {
                s.Bind(ie);
                Console.WriteLine($"Servidor en el puerto {port}");
                s.Listen(5);
                while (connected)
                {
                    Socket sClient = s.Accept();
                    Thread t = new Thread(hiloCliente);
                    t.IsBackground = true;
                    t.Start(sClient);
                }
            }catch(SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine("Puerto ocupado");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            s.Close();
        }

        internal void hiloCliente(object socket)
        {
            Socket sClient = (Socket)socket;
            string ip = "";
            IPHostEntry hostInfo = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipHost in hostInfo.AddressList)
            {
                if(ipHost.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip = ipHost.ToString();
                }
            }

            IPEndPoint ieClient = (IPEndPoint)sClient.RemoteEndPoint;
            Console.WriteLine("Cliente IP: "+ip+" en puerto "+ieClient.Port);

            string msg = "";

            try
            {
                using (NetworkStream ns = new NetworkStream(sClient))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    sw.WriteLine("CONEXIÓN ESTABLECIDA");
                    sw.Flush();

                    while (msg != null)
                    {
                        msg = sr.ReadLine();
                        if (msg != null)
                        {
                            switch (msg.Split(' ')[0])
                            {
                                case "GET":
                                    int nLineas;
                                    if (msg.Split(' ').Length == 2 &&
                                        msg.Split(' ')[1].Length > 0 && msg.Split(' ')[1].Contains(",")
                                        && msg.Split(' ')[1].Split(',').Length == 2
                                        && Int32.TryParse(msg.Split(' ')[1].Split(',')[1], out nLineas))
                                    {
                                        lock (l)
                                        {
                                            sw.WriteLine(leeArchivo(msg.Split(' ')[1].Split(',')[0], nLineas));
                                            sw.Flush();
                                        }
                                    }
                                    break;
                                case "PORT":
                                    int port;
                                    if (msg.Split(' ').Length == 2 && Int32.TryParse(msg.Split(' ')[1], out port))
                                    {
                                        lock (l)
                                        {
                                            guardaPuerto(port);
                                        }
                                    }
                                    break;
                                case "LIST":
                                    lock (l)
                                    {
                                        sw.WriteLine(listaArchivos());
                                        sw.Flush();
                                    }
                                    break;
                                case "CLOSE":
                                    sClient.Close();
                                    break;
                                case "HALT":
                                    lock (l)
                                    {
                                        connected = false;
                                        s.Close();
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Cliente desconectado");
            }             
        }

    }

}
