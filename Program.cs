using System.Net.Mail;
using System.Net;
using OpenPop.Mime;
using OpenPop.Pop3;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;



/* Please Enter Your Account Information Below to Use this Application. Must Have Gmail Permissions for Computers to Use */
string YourAccount = "<YOUR EMAIL>";
string YourPassword = "<YOUR ACCESS CODE FROM GMAIL>";

byte[] key = GetSHA1KeyFromPassword("SEN320_MFALab");
int working = -1;
while(working < 0)
{
    Console.WriteLine("Welcome to the Multi Factor Authentication Application!");
    Console.WriteLine(@"Would you like to sign up or sign in?
1) Sign Up
2) Sign In
3) Exit");
    string choice = Console.ReadLine();
    switch(choice){
        case "1":
            SignUp();
            break;
        case "2":
            if(SignIn());
            working = 1;
            break;
        case "3":
            working = 1;
            break;
    }
}


void SignUp(){
    bool signed = false;
    while(!signed)
    {
        Console.WriteLine("Please enter a Username:");
        string? userName = Console.ReadLine();
        if(File.Exists(userName + ".json")){
            System.Console.WriteLine("Username already in use, try again");
        }
        else{
            Console.WriteLine("Please enter a Password:");
            string? pass = Console.ReadLine();
            pass = Encrypt(pass, key);
            Console.WriteLine("Please enter your Email Address:");
            string? email = Console.ReadLine();
            SendEmail(email, "Welcome to MFA", "Your Sign Up was Successfully Processed!");
            System.Console.WriteLine("Congrats! You're Signed Up");
            Account acc = new Account(userName, pass, email);
            string jsonStr = JsonConvert.SerializeObject(acc);
            using(var streamWriter = new StreamWriter(userName + ".json", true))
            {   
            streamWriter.WriteLine(jsonStr.ToString());
            streamWriter.Close();
            }
            signed = true;
        }
    }
    
}

bool SignIn()
{
    Stopwatch watch = new Stopwatch();
    Random rand = new Random();
    Console.WriteLine("Enter UserName:");
    string? userName = Console.ReadLine();
    if(!File.Exists(userName + ".json")){
        System.Console.WriteLine("User Doesn't Exist, Try Again");
    }else
    {
        Console.WriteLine("Enter Password:");
        string? pass = Console.ReadLine();
        pass = Encrypt(pass, key);
        string json = File.ReadAllText(userName + ".json");
        Account? acc = JsonConvert.DeserializeObject<Account>(json);
        if(userName.Equals(acc.userName) && pass.Equals(acc.pass))
        {
            string code = "";
            char letter;
            for(int i = 0; i < 6; i++)
            {
                letter = Convert.ToChar(rand.Next(0,26) + 65);
                code += letter;
            }
            SendEmail(acc.email, "MFA Code", code);
            System.Console.WriteLine("Enter the Passcode Sent to Your Email:");
            watch.Start();
            string? userCode = Console.ReadLine();
            if(userCode.Equals(code) && watch.ElapsedMilliseconds < 300000)
            {
                System.Console.WriteLine("Congrats on Signing in via MFA");
                watch.Stop();
                return true;
            }
            else
            {
                System.Console.WriteLine("Login Failed... Try Again");
                return false;
            }
        }else
        {
            System.Console.WriteLine($"Password Didn't Match for User: {userName}");
            return false;
        }
    }
    return false;
}


/////////////Email//////////////
void SendEmail(string? to, string? subject, string? content)
{
    string fromAddress = YourAccount;
    string fromPassword = YourPassword;
    using(SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
    {
        smtp.EnableSsl = true;
        smtp.UseDefaultCredentials = false;
        smtp.Credentials = new NetworkCredential(fromAddress, fromPassword);
        smtp.Send(fromAddress, to, subject, content);
    }
}

////////////////////////Encryption////////////////////
byte [] GetSHA1KeyFromPassword(string password)
{
    using(SHA1 sha1Hash = SHA1.Create())
    {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(password);
        byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
        // Console.Write(hashBytes);

        return hashBytes.Take(16).ToArray();
    }
}

string Encrypt(string plainText, byte[] key){
    using(var aes = Aes.Create())
    {
        byte[] encoded = Encoding.UTF8.GetBytes(plainText);
        aes.Key = key;
        byte[] encrypted = aes.EncryptEcb(encoded, PaddingMode.PKCS7);
        return Convert.ToBase64String(encrypted);
    }
}

string Decrypt(string base64, byte[] key)
{
    using(var aes = Aes.Create())
    {
        var decoded = Convert.FromBase64String(base64);
        aes.Key = key;
        byte[] decrypted = aes.DecryptEcb(decoded, PaddingMode.PKCS7);
        return Encoding.ASCII.GetString(decrypted);
    }
}



////////////////Classes////////////////
public class Account
{
    public string userName {get; set;}
    public string pass {get; set;}
    public string email {get; set;}
    public Account(string user, string pass, string email)
    {
        this.userName = user;
        this.pass = pass;
        this.email = email;
    }
}

