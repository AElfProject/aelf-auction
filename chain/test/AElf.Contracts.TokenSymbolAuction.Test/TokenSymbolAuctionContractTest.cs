using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenSymbolAuction
{
    public class TokenSymbolAuctionContractTest : TokenSymbolAuctionContractTestBase
    {
        [Fact]
        public async Task HelloCall_ReturnsTokenSymbolAuctionMessage()
        {
            var txResult = await TokenSymbolAuctionContractStub.Hello.SendAsync(new Empty());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var text = new HelloReturn();
            text.MergeFrom(txResult.TransactionResult.ReturnValue);
            text.Value.ShouldBe("Hello World!");
        }
    }
}