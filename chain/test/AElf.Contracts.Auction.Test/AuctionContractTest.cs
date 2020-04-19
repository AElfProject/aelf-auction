using System;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Auction
{
    public class AuctionContractTest : AuctionContractTestBase
    {
        [Fact]
        public async Task HelloCall_ReturnsAuctionMessage()
        {
            var txResult = await AuctionContractStub.Hello.SendAsync(new Empty());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var text = new HelloReturn();
            text.MergeFrom(txResult.TransactionResult.ReturnValue);
            text.Value.ShouldBe("Hello World!");
        }


        [Fact]
        public async Task Main()
        {
            var createResult = await AuctionContractStub.Create.SendAsync(new CreateDto()
            {
                Callback = AuctionContractAddress,
                Receiver = Address.FromPublicKey(this.DefaultKeyPair.PublicKey),
                ExpiredDate = TimestampHelper.GetUtcNow().AddSeconds(1),
                MinAmount = 10,
                TokenSymbol = "ELF"
            });
        }
    }
}