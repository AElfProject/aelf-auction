using System.IO;
using System.Linq;
using Acs0;
using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.Auction
{
    public class AuctionContractTestBase : ContractTestBase<AuctionContractTestModule>
    {
        internal AuctionContractContainer.AuctionContractStub AuctionContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        private ACS0Container.ACS0Stub ZeroContractStub { get; set; }

        protected Address AuctionContractAddress { get; set; }

        protected Address TokenContractAddress { get; set; }

        protected ECKeyPair DefaultKeyPair { get; set; } = SampleECKeyPairs.KeyPairs.First();


        protected AuctionContractTestBase()
        {
            InitializeContracts();
        }

        private void InitializeContracts()
        {
            ZeroContractStub = GetZeroContractStub(SampleECKeyPairs.KeyPairs.First());

            AuctionContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AuctionContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList =
                            new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                    })).Output;
            AuctionContractStub = GetAuctionContractStub(SampleECKeyPairs.KeyPairs.First());


            TokenContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GetTokenContractInitialMethodCallList()
                    })).Output;
            TokenContractStub = GetTokenContractStub(DefaultKeyPair);

            AsyncHelper.RunSync(async () =>
            {
                await AuctionContractStub.Initialize.SendAsync(new InitializeDto()
                {
                });
            });
        }

        private ACS0Container.ACS0Stub GetZeroContractStub(ECKeyPair keyPair)
        {
            return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        private AuctionContractContainer.AuctionContractStub GetAuctionContractStub(ECKeyPair keyPair)
        {
            return GetTester<AuctionContractContainer.AuctionContractStub>(AuctionContractAddress, keyPair);
        }

        private TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GetTokenContractInitialMethodCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            {
                Value =
                {
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(TokenContractStub.Create),
                        Params = new CreateInput
                        {
                            // Issuer assigned to zero contract in order to issue token after deployment.
                            Issuer = ContractZeroAddress,
                            Symbol = "ELF",
                            IsBurnable = true,
                            Decimals = 8,
                            IsProfitable = true,
                            TokenName = "Elf token.",
                            TotalSupply = 10_0000_0000_00000000
                        }.ToByteString()
                    },
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(TokenContractStub.Issue),
                        Params = new IssueInput
                        {
                            Symbol = "ELF",
                            Amount = 10_0000_0000_00000000,
                            To = Address.FromPublicKey(DefaultKeyPair.PublicKey)
                        }.ToByteString()
                    }
                }
            };
        }
    }
}