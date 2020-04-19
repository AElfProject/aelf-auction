using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
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
                Callback = new Address(),
                Receiver = Address.FromPublicKey(this.DefaultKeyPair.PublicKey),
                ExpiredDate = TimestampHelper.GetUtcNow().AddSeconds(1),
                MinAmount = 10,
                TokenSymbol = "ELF"
            });

            var id = createResult.Output.Id;

            var failedBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 1,
                Id = id
            });

            Assert.Equal(BidStatus.Rejected, failedBid.Output.Status);


            var vAddress = await AuctionContractStub.GetSenderVirtualAddress.CallAsync(new Empty());


            //Deposit to virtual address
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 20,
                To = vAddress,
                Symbol = "ELF"
            });

            var successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 11,
                Id = id
            });

            Assert.True(successBid.Output.Status == BidStatus.Awarded);

            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 12,
                Id = id
            });

            Assert.Equal(BidStatus.Awarded, successBid.Output.Status);


            var user1Address = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey);
        }
    }
}