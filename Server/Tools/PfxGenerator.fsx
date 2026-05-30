open System
open System.IO
open System.Security.Cryptography
open System.Security.Cryptography.X509Certificates

let host = if fsi.CommandLineArgs.Length > 1 then fsi.CommandLineArgs.[1] else "localhost"

use rsa = RSA.Create(2048)
let request = CertificateRequest("CN=" + host, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)

let sanBuilder = SubjectAlternativeNameBuilder()
match System.Net.IPAddress.TryParse(host) with
| true, ip -> sanBuilder.AddIpAddress(ip)
| false, _ -> sanBuilder.AddDnsName(host)
request.CertificateExtensions.Add(sanBuilder.Build())

let oids = OidCollection()
oids.Add(Oid("1.3.6.1.5.5.7.3.1")) |> ignore
oids.Add(Oid("1.3.6.1.5.5.7.3.2")) |> ignore

request.CertificateExtensions.Add(X509EnhancedKeyUsageExtension(oids, false))

let cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(99))
File.WriteAllBytes(host + ".pfx", cert.Export(X509ContentType.Pfx, ""))

printfn "%s" (Path.GetFullPath(host + ".pfx"))

// How to use:  dotnet fsi PfxGenerator.fsx tm.eugnet.com