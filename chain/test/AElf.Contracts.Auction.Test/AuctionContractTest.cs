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


            var vUserAddress = await AuctionContractStub.GetSenderVirtualAddress.CallAsync(new Empty());


            //Deposit to virtual address
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 40,
                To = vUserAddress,
                Symbol = "ELF"
            });


            await CheckBalance(vUserAddress, 40);

            var successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 11,
                Id = id
            });

            Assert.True(successBid.Output.Status == BidStatus.Awarded);

            await CheckBalance(vUserAddress, 29);


            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 12,
                Id = id
            });

            await CheckBalance(vUserAddress, 28);


            Assert.Equal(BidStatus.Awarded, successBid.Output.Status);

            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 30,
                Id = id
            });

            await CheckBalance(vUserAddress, 10); // 10 = 28 + 12 - 30

            failedBid = await AuctionContractStub.Bid.SendWithExceptionAsync(new BidDto()
            {
                Amount = 50,
                Id = id
            });

            failedBid.TransactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();

            await CheckBalance(vUserAddress, 10);


            var user1Address = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey);
        }

        private async Task CheckBalance(Address vAddress, long expect)
        {
            var balanceOutput =
                await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                    {Owner = vAddress, Symbol = "ELF"});

            Assert.Equal(expect, balanceOutput.Balance);
        }
    }
}