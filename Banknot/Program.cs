using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle;

namespace Banknot {
     class Program {
          static void Main( string[] args ) {


               Alice alice = new Alice(100);
               Bank bank = new Bank();

               int checkedBanknoteFromBank = 0;

               bank.GetInformationAboutServer();
               alice.Connect("10.100.5.236", 8001);
               bank.AcceptConnection();
               alice.GetStreamFromServer();

               bank.GenerateRandomIdForAlice();
               bank.SendBanknoteID();
               alice.ReceiveClientID();

               bank.SendPublicKeyExponent();
               alice.ReceivePublicKeyExponent();

               bank.SendPublicKeyModulus();
               alice.ReceivePublicKeyModulus();

               alice.GenerateBanknotes("100", "RaiffeisenPolbank");

               bank.ShowOutput();

               for (int i = 0; i<100; i++) {
                    
                    alice.GenerateHashedBanknote(i);
                    alice.SendGeneratedHashedBanknote(i);
                    bank.ReceiceHashedBanknote(i);
                    
               }

               bank.SendSelectedBanknote();
               checkedBanknoteFromBank = alice.ReceiveCheckedBanknoteFromBank();

               for(int i = 0; i< 100; i++) {
                    if(i == checkedBanknoteFromBank) {
                         continue;
                    } else {
                         alice.SendClientBankID(i);
                         bank.ReceiveClientBankID(i);

                         alice.SendValueOfBanknote(i);
                         bank.ReceiveValueOfBanknote(i);

                         alice.SendIdOfBanknote(i);
                         bank.ReceiveIdOfBanknote(i);

                         alice.SendNameBank(i);
                         bank.ReceiveNameBank(i);

                         bank.InitlializeBanknote(i);
                         for (int j = 0; j < 100; j++) {

                              alice.SendRandomBiteSeries(i, j);
                              bank.ReceiveRandomBiteSeries(i, j);

                              alice.SendHashRandomSeries(i, j);
                              bank.ReceiveHashRandomSeries(i, j);

                              alice.SendHashXOROperation(i, j);
                              bank.ReceiveHashXOROperation(i, j);
                         }
                    }
               }
               bank.CheckBanknotes(alice._listOfBanknotes, alice._listOfHashedBanknote);
               bank.CloseServer();
               alice.CloseTCPClient();

               Console.ReadKey();
          }
     }
}


