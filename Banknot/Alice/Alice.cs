using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using Banknot.Model;
using System.Numerics;
using System.Xml;

namespace Banknot {
     public class Alice{

          public byte[][] _listOfHashedBanknote;
          private byte[] _convertASCIIToByte, _answerFromServer, _textForServer, _clientID;
          private int _amountOfMoney, _checkedBanknoteFromBank;
          private XmlDocument _generatedXML;
          private Random _numberGenerator;
          private BigInteger _modulus, _exponent, _d;


          [Serializable]
          public struct Banknote {
               public byte[] _clientBankID, _bank, _idOfBanknote, _value;
               public byte[][] _randomBiteSeries, _hashOfRandomSeries, _hashOfXOROperation;
          }
          
          [Serializable]
          private struct PublicRSAKeyFromBank {
               public byte[] _exponent, _modulus;
          }

          
          public Banknote[] _listOfBanknotes;

          private PublicRSAKeyFromBank _publicKey;
          private SHA256CryptoServiceProvider _hashFunction;
          

          private TcpClient _tcpClient;
          private Stream _serverStream;
          private ASCIIEncoding _encodeOutputFromServer;

          private MemoryStream _memoryStream;
          private BinaryFormatter _bianaryStream;


          public Alice(int amountOfMoney) {

               _amountOfMoney = amountOfMoney;
               _numberGenerator = new Random();
               _listOfBanknotes = new Banknote[100];
               _listOfHashedBanknote = new byte[100][];

               HashFunction = new SHA256CryptoServiceProvider();
               _publicKey = new PublicRSAKeyFromBank();

               TCPClient = new TcpClient();
               ASCII = new ASCIIEncoding();
               ServerAnswer = new byte[512];
               
               _memoryStream = new MemoryStream();
               _bianaryStream = new BinaryFormatter();
               
               
               
               

          }

          ASCIIEncoding ASCII { get { return _encodeOutputFromServer; } set { _encodeOutputFromServer = value; } }
          Stream ServerStream { get { return _serverStream; } set { _serverStream = value; } }
          TcpClient TCPClient { get { return _tcpClient; } set { _tcpClient = value; } }
          SHA256CryptoServiceProvider HashFunction { get { return _hashFunction; } set { _hashFunction = value; } }
          byte[] ServerAnswer { get { return _answerFromServer; } set { _answerFromServer = value; } }
          BinaryFormatter BinarySerlialization { get { return _bianaryStream; } set { _bianaryStream = value; } }
          MemoryStream MemoryStream { get { return _memoryStream; } set { _memoryStream = value; } }
          XmlDocument XML { get { return _generatedXML; } set { _generatedXML = value; } }
          

          public void ReceivePublicKeyModulus() {

               int _amountReceivedData = 0;
               byte[] _buffer;
               _amountReceivedData = ServerStream.Read(ServerAnswer, 0, ServerAnswer.Length);
               _buffer = new byte[_amountReceivedData];
               Array.Copy(ServerAnswer, _buffer, _amountReceivedData);
               //Array.Reverse(ServerAnswer);
               _modulus = new BigInteger(_buffer);

               if(_modulus.Sign == -1) {
                    _modulus = _modulus * BigInteger.MinusOne;
               }
          }
          public void ReceivePublicKeyExponent() {
               int _amountReceivedData = 0;
               byte[] _buffer;
               _amountReceivedData = ServerStream.Read(ServerAnswer, 0, ServerAnswer.Length);

               _buffer = new byte[_amountReceivedData];
               Array.Copy(ServerAnswer, _buffer, _amountReceivedData);
               _exponent = new BigInteger(_buffer);

               if(_exponent.Sign == -1) {
                    _exponent = _exponent * BigInteger.MinusOne;
               }
          }

          public void ReceiveClientID() {

               int _amountReceivedData = 0;
               _amountReceivedData = ServerStream.Read(ServerAnswer, 0, ServerAnswer.Length);
               _clientID = new byte[_amountReceivedData];

               for (int i = 0; i < _amountReceivedData; i++) {
                    _clientID[i] = ServerAnswer[i];
               }

               ServerStream.Flush();
               Console.WriteLine("Alice - Answer was received");
               
          }

          public int ReceiveCheckedBanknoteFromBank() {
               int _amountReceivedData = 0;
               _amountReceivedData = ServerStream.Read(ServerAnswer, 0, ServerAnswer.Length);
               _checkedBanknoteFromBank = BitConverter.ToInt32(ServerAnswer, 0);

               return _checkedBanknoteFromBank;

          }
        public void GenerateBanknotes(string valueOfBanknote, string nameOfBank) {

               for (int i = 0; i <100; i++) {

                    byte[] _byteArrayIdOfBanknote, _byteArrayValueOfBanknote, _byteArrayNameOfBank;
                    byte _valuePXor, _valueQXor;

                    _listOfBanknotes[i]._clientBankID = _clientID;
                    _listOfBanknotes[i]._randomBiteSeries = new byte[100][];
                    _listOfBanknotes[i]._hashOfXOROperation = new byte[100][];
                    _listOfBanknotes[i]._hashOfRandomSeries = new byte[100][];

                    for (int y = 0; y < 100; y++) {
                         _listOfBanknotes[i]._randomBiteSeries[y] = new byte[100];
                         _listOfBanknotes[i]._hashOfXOROperation[y] = new byte[100];
                         _listOfBanknotes[i]._hashOfRandomSeries[y] = new byte[100];
                         for (int j = 0; j < 100; j++) {

                              _listOfBanknotes[i]._randomBiteSeries[y][j] = (byte)_numberGenerator.Next(0, 2);
                         }
                         _listOfBanknotes[i]._hashOfRandomSeries[y] = HashFunction.ComputeHash(_listOfBanknotes[i]._randomBiteSeries[y]); 

                         _valuePXor = (byte)_numberGenerator.Next(0, 2);
                         _valueQXor = (byte)_numberGenerator.Next(0, 2);
                         if((_valueQXor == 0 && _valuePXor == 1) || (_valueQXor == 1 && _valuePXor == 0)) {
                              _listOfBanknotes[i]._hashOfXOROperation[y] = HashFunction.ComputeHash(_clientID);
;                         } else {
                              _listOfBanknotes[i]._hashOfXOROperation[y] = HashFunction.ComputeHash(_listOfBanknotes[i]._randomBiteSeries[y]);
                         }
                    }

                    if (BitConverter.IsLittleEndian) {
                         _byteArrayIdOfBanknote = BitConverter.GetBytes(i);
                         _byteArrayNameOfBank = ASCII.GetBytes(nameOfBank);
                         _byteArrayValueOfBanknote = ASCII.GetBytes(valueOfBanknote);
                         Array.Reverse(_byteArrayValueOfBanknote);
                         Array.Reverse(_byteArrayNameOfBank);
                         Array.Reverse(_byteArrayIdOfBanknote);
                         _listOfBanknotes[i]._idOfBanknote = _byteArrayIdOfBanknote;
                         _listOfBanknotes[i]._value = _byteArrayValueOfBanknote;
                         _listOfBanknotes[i]._bank = _byteArrayNameOfBank;

                    } else {
                         _byteArrayIdOfBanknote = BitConverter.GetBytes(i);
                         _byteArrayNameOfBank = ASCII.GetBytes(nameOfBank);
                         _byteArrayValueOfBanknote = ASCII.GetBytes(valueOfBanknote);
                         _listOfBanknotes[i]._idOfBanknote = _byteArrayIdOfBanknote;
                         _listOfBanknotes[i]._value = _byteArrayValueOfBanknote;
                         _listOfBanknotes[i]._bank = _byteArrayNameOfBank;
                    }
               }
          }

          public void GenerateHashedBanknote(int index) {

               _bianaryStream.Serialize(_memoryStream, _listOfBanknotes[index]);
               byte[] _buffer = new byte[_memoryStream.Length];
               int _lengthOfMemoryStream = (int)_memoryStream.Length;
               _memoryStream.Position = 0;
               _memoryStream.Read(_buffer, 0, (int)_memoryStream.Length);
               
               //obfuscation of Baknote
               BigInteger m = new BigInteger(_buffer);
               BigInteger k = new BigInteger(2);
               //BigInteger k_prim = new BigInteger();
               BigInteger m_prim = m * BigInteger.Pow(k, 65537) % _modulus;

               _memoryStream.SetLength(0);
               _bianaryStream.Serialize(_memoryStream, m_prim);
               _buffer = new byte[_memoryStream.Length];
               _memoryStream.Position = 0;
               _memoryStream.Read(_buffer, 0, (int)_memoryStream.Length);
               _listOfHashedBanknote[index] = new byte[_buffer.Length];
               Array.Copy(_buffer, _listOfHashedBanknote[index], _buffer.Length);

               //generowanie podpisu
            //   BigInteger s_prim = BigInteger.ModPow(m_prim, _d, _modulus);
               //zdejmowanie zaciemnienia
              // BigInteger s = k_prim * s_prim % _modulus;
          }

          public void SendGeneratedHashedBanknote(int indexOfBanknote) {
               ServerStream.Write(_listOfHashedBanknote[indexOfBanknote], 0, _listOfHashedBanknote[indexOfBanknote].Length);
          }

          public void SendHashRandomSeries(int indexBanknote, int indexOfElement) {
               ServerStream.Write(_listOfBanknotes[indexBanknote]._hashOfRandomSeries[indexOfElement], 0, _listOfBanknotes[indexBanknote]._hashOfRandomSeries[indexOfElement].Length);
          }
          public void SendHashXOROperation( int indexBanknote, int indexOfElement ) {
               ServerStream.Write(_listOfBanknotes[indexBanknote]._hashOfXOROperation[indexOfElement], 0, _listOfBanknotes[indexBanknote]._hashOfXOROperation[indexOfElement].Length);
          }
          public void SendRandomBiteSeries( int indexBanknote, int indexOfElement ) {
               ServerStream.Write(_listOfBanknotes[indexBanknote]._randomBiteSeries[indexOfElement], 0, _listOfBanknotes[indexBanknote]._randomBiteSeries[indexOfElement].Length);
          }
          public void SendClientBankID( int index ) {
               ServerStream.Write(_listOfBanknotes[index]._clientBankID, 0, _listOfBanknotes[index]._clientBankID.Length);
          }
          public void SendNameBank( int index ) {
               ServerStream.Write(_listOfBanknotes[index]._bank, 0, _listOfBanknotes[index]._bank.Length);
          }
          public void SendIdOfBanknote( int index ) {
               ServerStream.Write(_listOfBanknotes[index]._idOfBanknote, 0, _listOfBanknotes[index]._idOfBanknote.Length);
          }
          public void SendValueOfBanknote( int index ) {
               ServerStream.Write(_listOfBanknotes[index]._value, 0, _listOfBanknotes[index]._value.Length);
          }

          //TCP CLIENT
          public void Connect( string ip, int port ) {
               TCPClient.Connect(ip, port);
          }

          public void GetStreamFromServer() {
               ServerStream = TCPClient.GetStream();
          }

          public void SendData() {
               ServerStream.Write(_textForServer, 0, _textForServer.Length);
          }

          public int ReceiveData() {

               int _amountReceivedData = 0;
               _amountReceivedData = ServerStream.Read(ServerAnswer, 0, 512);
               for (int i = 0; i < _amountReceivedData; i++) {
                    Console.Write(Convert.ToChar(ServerAnswer[i]));
               }
               Console.WriteLine();
               Console.WriteLine("Alice - Answer was received");
               return _amountReceivedData;
          }

          public void CloseTCPClient() {
               TCPClient.Close();
          }
          
     }
}
