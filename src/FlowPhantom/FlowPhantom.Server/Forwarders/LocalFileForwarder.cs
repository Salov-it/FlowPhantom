using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Server.Forwarders
{
    public class LocalFileForwarder
    {
        private readonly string _folder;

        public LocalFileForwarder(string folder)
        {
            _folder = folder;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public async Task ForwardAsync(byte[] data, CancellationToken ct)
        {
            string name = $"{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.bin";
            string path = Path.Combine(_folder, name);

            await File.WriteAllBytesAsync(path, data, ct);
            Console.WriteLine($"[FORWARD] Saved: {path}");
        }
    }
}
