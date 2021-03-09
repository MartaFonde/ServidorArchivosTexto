using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorArchivosTexto
{
    class Program
    {
        static void Main(string[] args)
        {
            ServidorArchivos server = new ServidorArchivos();
            server.iniciaServidorArchivos();
        }
    }
}
