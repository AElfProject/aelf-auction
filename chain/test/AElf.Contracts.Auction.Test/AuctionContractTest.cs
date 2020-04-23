using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CallerContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

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
        public async Task TestCaller()
        {
            var createResult = await CallerContractStub.Create.SendAsync(new Empty() { });


            var id = createResult.Output;
            var auction = await AuctionContractStub.GetAuction.CallAsync(id);

            auction.Status.ShouldBe(AuctionStatus.Active);

            var vUserAddress = await AuctionContractStub.GetSenderVirtualAddress.CallAsync(new Empty());

            //Deposit 40 to virtual address
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 40,
                To = vUserAddress,
                Symbol = "ELF"
            });

            var blockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();


            var successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 11,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Awarded);

            //this time, auction is expired, so we will release the auction, fire success event 
            blockTimeProvider.SetBlockTime(TimestampHelper.GetUtcNow().AddSeconds(200));

            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 12,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Rejected);


            //check auction success log event
            var successLogEvent = successBid.TransactionResult.Logs.First(
                l => l.Name.Contains(nameof(AuctionSuccessEvent)));

            var auctionSuccessEvent = new AuctionSuccessEvent();
            auctionSuccessEvent.MergeFrom(successLogEvent);

            auctionSuccessEvent.Amount.ShouldBe(11);

            var callbackLogEvent = successBid.TransactionResult.Logs.First(
                l => l.Name.Contains(nameof(CallbackEvent)));


            var callbackEvent = new CallbackEvent();
            callbackEvent.MergeFrom(callbackLogEvent);

            callbackEvent.AuctionId.ShouldBe(id);
        }


        [Fact]
        public async Task Main()
        {
            var createResult = await AuctionContractStub.Create.SendAsync(new CreateDto()
            {
                Callback = new Address(),
                Receiver = Address.FromPublicKey(this.DefaultKeyPair.PublicKey),
                ExpiredDate = TimestampHelper.GetUtcNow().AddSeconds(100),
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


            //Deposit 40 to virtual address
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 40,
                To = vUserAddress,
                Symbol = "ELF"
            });


            await CheckBalance(vUserAddress, 40);

            //bid 11
            var successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 11,
                Id = id
            });

            Assert.True(successBid.Output.Status == BidStatus.Awarded);

            await CheckBalance(vUserAddress, 29);


            //bid 11 again, will be rejected
            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 11,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Rejected);

            //bid 12, success 
            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 12,
                Id = id
            });

            await CheckBalance(vUserAddress, 28);


            Assert.Equal(BidStatus.Awarded, successBid.Output.Status);


            //bid 30, balance is 28, but will refund 12, so it will also success
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

            //another user join the auction
            var user1Address = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey);

            var user1AuctionContractStub = GetAuctionContractStub(SampleECKeyPairs.KeyPairs[1]);

            var user1VAddress = await user1AuctionContractStub.GetSenderVirtualAddress.CallAsync(new Empty());

            //deposit 100
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 100,
                Symbol = "ELF",
                To = user1VAddress
            });

            await CheckBalance(user1VAddress, 100);

            //test withdraw
            await user1AuctionContractStub.Withdraw.SendAsync(new WithdrawDto() {Symbol = "ELF"});

            await CheckBalance(user1VAddress, 0);
            await CheckBalance(user1Address, 100);


            //deposit 200
            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 200,
                Symbol = "ELF",
                To = user1VAddress
            });


            //bid for 30, as lastBidder is also 30, be rejected
            successBid = await user1AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 30,
                Id = id
            });


            successBid.Output.Status.ShouldBe(BidStatus.Rejected);

            successBid = await user1AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 31,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Awarded);

            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 31,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Rejected);


            successBid = await AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 32,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Awarded);

            successBid = await user1AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 33,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Awarded);

            var blockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();

            //this time, auction is expired, so we will release the auction, fire success event 
            blockTimeProvider.SetBlockTime(TimestampHelper.GetUtcNow().AddSeconds(100));

            successBid = await user1AuctionContractStub.Bid.SendAsync(new BidDto()
            {
                Amount = 35,
                Id = id
            });

            successBid.Output.Status.ShouldBe(BidStatus.Rejected);


            //check auction success log event

            var successLogEvent = successBid.TransactionResult.Logs.First(
                l => l.Name.Contains(nameof(AuctionSuccessEvent)));


            var auctionSuccessEvent = new AuctionSuccessEvent();
            auctionSuccessEvent.MergeFrom(successLogEvent);

            auctionSuccessEvent.Amount.ShouldBe(33);
            auctionSuccessEvent.Bidder.ShouldBe(user1Address);
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