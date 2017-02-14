using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Signers;
using System.IO;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;


namespace Banknot {
     public class Bank {

          
          private byte[] _answerFromClient, _randomIDForAlice;
          private byte[][] _listOfHashedBanknote;
          private int _amountOfReceivedData, _checkedBanknote;
          private Random _numberGenerator;
          private BigInteger _modulus, _exponent, _d;
          
          [Serializable]
          private struct Banknote {
               public byte[] _clientBankID, _bank, _idOfBanknote, _value;
               public byte[][] _randomBiteSeries, _hashOfRandomSeries, _hashOfXOROperation;
          }
          [Serializable]
          private struct PublicRSAKey {
               public byte[] _exponent, _modulus;
          }
          [Serializable]
          private struct PrivateRSAKey {
               public byte[] _d, _modulus;
          }

          private Banknote[] _listOfBanknotes;
          private PublicRSAKey _publicKey;
          private PrivateRSAKey _privateKey;

          private RSACryptoServiceProvider _rsaAlgorihtm;
          private SHA256CryptoServiceProvider _hashFunction;
          private CspParameters _cspParameters;
          private RSAParameters _rsakeys;

          private IPAddress _ipAdress;
          private TcpListener _serverSocket;
          private Socket _connectedSocketWithClient;
          private ASCIIEncoding _textForClient;

          private MemoryStream _memoryStream;
          private BinaryFormatter _bianaryStream;
          private XmlDocument _generatedXML;

          public Bank() {

               _amountOfReceivedData = 0;
               _numberGenerator = new Random();
               _textForClient = new ASCIIEncoding();
               _randomIDForAlice = new byte[100];
               _listOfBanknotes = new Banknote[100];
               _listOfHashedBanknote = new byte[100][];
               _publicKey = new PublicRSAKey();
               _privateKey = new PrivateRSAKey();

               _memoryStream = new MemoryStream();
               _bianaryStream = new BinaryFormatter();

               IPAdress = IPAddress.Parse("10.100.5.236");
               ServerSocket = new TcpListener(IPAdress, 8001);
               ClientAnswer = new byte[20000];
               HashFunction = new SHA256CryptoServiceProvider();

               RSA = new RSACryptoServiceProvider(384);
               _rsakeys = RSA.ExportParameters(true);

               if (BitConverter.IsLittleEndian) {

                    byte[] _byteArrayExponent, _byteArrayModulus, _byteArrayD;

                    
                    _privateKey._d = _rsakeys.D;
                    _privateKey._modulus = _rsakeys.Modulus;

                    _byteArrayExponent = _rsakeys.Exponent;
                    _byteArrayModulus = _rsakeys.Modulus;
                    _byteArrayD = _rsakeys.D;
                    Array.Reverse(_byteArrayExponent);
                    Array.Reverse(_byteArrayModulus);
                    Array.Reverse(_byteArrayD);

                    _publicKey._exponent = _rsakeys.Exponent;
                    _publicKey._modulus = _rsakeys.Modulus;

                    _modulus = new BigInteger(_byteArrayModulus);
                    _exponent = new BigInteger(_byteArrayExponent);
                    _d = new BigInteger(_byteArrayD);
                    if (_modulus.Sign == -1) {
                         _modulus = _modulus * BigInteger.MinusOne;
                    }
                    if (_exponent.Sign == -1) {
                         _exponent = _exponent * BigInteger.MinusOne;
                    }
                    if (_d.Sign == -1) {
                         _d = _d * BigInteger.MinusOne;
                    }

               }

               _generatedXML = new XmlDocument();
               _generatedXML.LoadXml(RSA.ToXmlString(true));
               XmlNodeList modulus = _generatedXML.GetElementsByTagName("Modulus");
               XmlNodeList d = _generatedXML.GetElementsByTagName("D");
               XmlNodeList exponent = _generatedXML.GetElementsByTagName("Exponent");
               Console.WriteLine("***********************************************************");
               Console.WriteLine("RSA parameters in bytes: ");
               Console.WriteLine();
               Console.WriteLine("Modulus: {0}", modulus[0].InnerXml);
               Console.WriteLine("D: {0}", d[0].InnerXml);
               Console.WriteLine("Exponent: {0}", exponent[0].InnerXml);
               Console.WriteLine();
               Console.WriteLine("***********************************************************");

               //Console.WriteLine("Key is : \n" + RSA.ToXmlString(true));
               RSA.Clear();
               RSA.Dispose();

          }

          IPAddress IPAdress { get { return _ipAdress; } set { _ipAdress = value; } }
          TcpListener ServerSocket { get { return _serverSocket; } set { _serverSocket = value; } }
          Socket ClientSocket { get { return _connectedSocketWithClient; } set { _connectedSocketWithClient = value; }  }
          ASCIIEncoding TextForClient { get { return _textForClient; } set { _textForClient = value; } }
          RSACryptoServiceProvider RSA { get { return _rsaAlgorihtm; } set { _rsaAlgorihtm = value; } }
          XmlDocument XML { get { return _generatedXML; } set { _generatedXML = value; } }
          SHA256CryptoServiceProvider HashFunction { get { return _hashFunction; } set { _hashFunction = value; } }
          byte[] ClientAnswer { get { return _answerFromClient; } set { _answerFromClient = value; } }
          RSAParameters RSAKeys { get { return _rsakeys; } set { _rsakeys = value; } }
          PublicRSAKey PublicKey { get { return _publicKey; } set { _publicKey = value; } }
          PrivateRSAKey PrivateKey { get { return _privateKey; } set { _privateKey = value; } }

          
          //GENERATING ELECTRONIC CASHE
          public void GenerateRandomIdForAlice() {
               for(int i = 0; i < 100; i++) {
                    _randomIDForAlice[i] = (byte) _numberGenerator.Next(0, 2);
               }
               //Console.WriteLine("Bank was generated IDBanknote: {0}", _randomIDForAlice);
          }

          public void SendBanknoteID() {
               ClientSocket.Send(_randomIDForAlice);
          }

          public void SendPublicKeyExponent() { 

               ClientSocket.Send(PublicKey._exponent);
          }
          public void SendPublicKeyModulus() {
               ClientSocket.Send(PublicKey._modulus);
          }

          public void SendSelectedBanknote() {
               _checkedBanknote = _numberGenerator.Next(0, 100);
               ClientSocket.Send(BitConverter.GetBytes(_checkedBanknote));
               Console.WriteLine("Text was sent");
          }


          public void CheckBanknotes( Alice.Banknote[] _listOfBanknotesAlice, byte[][] listOfHashedBanknote) {
               for (int i = 0; i < 100; i++) {

                    if (i == _checkedBanknote) {
                         continue;
                    } else {
                         
                         _bianaryStream.Serialize(_memoryStream, _listOfBanknotesAlice[i]);
                         int _lengthOfMemoryStream = (int)_memoryStream.Length;
                         byte[] _buffer = new byte[_lengthOfMemoryStream];
                         _memoryStream.Position = 0;
                         _memoryStream.Read(_buffer, 0, _lengthOfMemoryStream);
                         
                         //obfuscation of Baknote
                         BigInteger m = new BigInteger(_buffer);
                         BigInteger k = new BigInteger(2);
                         BigInteger m_prim = m * BigInteger.Pow(k, 65537) % _modulus;

                         _memoryStream.SetLength(0);
                         _bianaryStream.Serialize(_memoryStream, m_prim);
                         _buffer = new byte[_memoryStream.Length];
                         _memoryStream.Position = 0;
                         _memoryStream.Read(_buffer, 0, (int)_memoryStream.Length);
                         BigInteger m_1 = new BigInteger(_buffer);
                         BigInteger k_1 = new BigInteger(2);
                         BigInteger m_prim_1 = m * BigInteger.Pow(k, 65537) % _modulus;

                         /*for(int j = 0; j <229; j++) {
                              if(_buffer[j] != _listOfHashedBanknote[i][j]) {
                                   Console.WriteLine("Element is diffrent is {0} index, value of _buffer {1}, _listhashed = {2}", j, _buffer[j], _listOfHashedBanknote[i][j]);
                                   Console.ReadKey();
                              }
                         }
                         */
                         if (m_prim == m_prim_1) {
                              Console.WriteLine("Generated {0} banknote is true", i);
                         } else
                              Console.WriteLine("Generated {0} banknote is false", i);
                    }
               }
               Console.WriteLine("Checked banknote = {0}", _checkedBanknote);
          }

          //TCP server. Methods are written in some order in which they should be run.

          public void GetInformationAboutServer() {
               Console.WriteLine("Server is running on : " + ServerSocket.LocalEndpoint);
               Console.WriteLine("Waiting for connection...");
               Console.WriteLine("************************************************************");
               Console.WriteLine();
               ServerSocket.Start();
          }

          public void AcceptConnection() {
               ClientSocket = ServerSocket.AcceptSocket();
               Console.WriteLine("Connection was accepted from : " + ClientSocket.RemoteEndPoint);
          }

          public void ReceiceHashedBanknote(int indexOfBanknote) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfHashedBanknote[indexOfBanknote] = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfHashedBanknote[indexOfBanknote], _amountOfReceivedData);
          }

          public void InitlializeBanknote(int indexOfBanknote ) {
               _listOfBanknotes[indexOfBanknote]._hashOfRandomSeries = new byte[100][];
               _listOfBanknotes[indexOfBanknote]._hashOfXOROperation = new byte[100][];
               _listOfBanknotes[indexOfBanknote]._randomBiteSeries = new byte[100][];

          }

          public void ReceiveHashRandomSeries(int indexOfBanknote, int indexOfElement) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[indexOfBanknote]._hashOfRandomSeries[indexOfElement] = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[indexOfBanknote]._hashOfRandomSeries[indexOfElement], _amountOfReceivedData);

          }
          public void ReceiveHashXOROperation( int indexOfBanknote, int indexOfElement ) {

               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[indexOfBanknote]._hashOfXOROperation[indexOfElement]= new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[indexOfBanknote]._hashOfXOROperation[indexOfElement], _amountOfReceivedData);
          }
          public void ReceiveRandomBiteSeries(int indexOfBanknote, int indexOfElement) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[indexOfBanknote]._randomBiteSeries[indexOfElement] = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[indexOfBanknote]._randomBiteSeries[indexOfElement], _amountOfReceivedData);
          }
          public void ReceiveClientBankID( int index ) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[index]._clientBankID = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[index]._clientBankID, _amountOfReceivedData);
          }
          public void ReceiveNameBank( int index ) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[index]._bank = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[index]._bank, _amountOfReceivedData);
          }
          public void ReceiveIdOfBanknote( int index ) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[index]._idOfBanknote = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[index]._idOfBanknote, _amountOfReceivedData);
          }
          public void ReceiveValueOfBanknote( int index ) {
               _amountOfReceivedData = ClientSocket.Receive(ClientAnswer);
               _listOfBanknotes[index]._value = new byte[_amountOfReceivedData];
               Array.Copy(ClientAnswer, _listOfBanknotes[index]._value, _amountOfReceivedData);
          }

          public void ShowOutput() {

               for(int i = 0; i < _amountOfReceivedData; i++) {
                    Console.Write(Convert.ToChar(ClientAnswer[i]));
               }
               Console.WriteLine("Answer was received");
          }


          public void SendData(string text) {
               ClientSocket.Send(TextForClient.GetBytes(text));
               Console.WriteLine("Text was sent");
          }

          public void CloseServer() {
               ServerSocket.Stop();
               ClientSocket.Close();
          }

          


     }
}
