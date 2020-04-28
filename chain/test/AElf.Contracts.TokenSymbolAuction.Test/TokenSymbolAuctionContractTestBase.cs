using System.IO;
using System.Linq;
using Acs0;
using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.TokenSymbolAuction
{
    public class TokenSymbolAuctionContractTestBase : ContractTestBase<TokenSymbolAuctionContractTestModule>
    {
        internal TokenSymbolAuctionContractContainer.TokenSymbolAuctionContractStub TokenSymbolAuctionContractStub { get; set; }
        private ACS0Container.ACS0Stub ZeroContractStub { get; set; }

        private Address TokenSymbolAuctionContractAddress { get; set; }

        protected TokenSymbolAuctionContractTestBase()
        {
            InitializeContracts();
        }

        private void InitializeContracts()
        {
            ZeroContractStub = GetZeroContractStub(SampleECKeyPairs.KeyPairs.First());

            TokenSymbolAuctionContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenSymbolAuctionContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList =
                            new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                    })).Output;
            TokenSymbolAuctionContractStub = GetTokenSymbolAuctionContractStub(SampleECKeyPairs.KeyPairs.First());
        }

        private ACS0Container.ACS0Stub GetZeroContractStub(ECKeyPair keyPair)
        {
            return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        private TokenSymbolAuctionContractContainer.TokenSymbolAuctionContractStub GetTokenSymbolAuctionContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenSymbolAuctionContractContainer.TokenSymbolAuctionContractStub>(TokenSymbolAuctionContractAddress, keyPair);
        }
    }
}