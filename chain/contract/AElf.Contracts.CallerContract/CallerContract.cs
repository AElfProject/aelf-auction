using AElf.Contracts.Auction;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CallerContract
{
    /// <summary>
    /// The C# implementation of the contract defined in caller_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public class CallerContract : CallerContractContainer.CallerContractBase
    {
        /// <summary>
        /// The implementation of the Hello method. It takes no parameters and returns on of the custom data types
        /// defined in the protobuf definition file.
        /// </summary>
        /// <param name="input">Empty message (from Protobuf)</param>
        /// <returns>a HelloReturn</returns>
        public override HelloReturn Hello(Empty input)
        {
            return new HelloReturn {Value = "Hello World!"};
        }

        public override Hash Create(Empty input)
        {
            var productId = Context.GenerateId();


            var auctionId = Context.GenerateId(State.AuctionContract.Value, null);

            State.AuctionContract.Create.Send(new CreateDto()
            {
                Callback = Context.Self,
                Receiver = Context.Sender,
                ExpiredDate = Context.CurrentBlockTime.AddSeconds(100),
                MinAmount = 1,
                TokenSymbol = "ELF"
            });


            State.Products[productId] = new Product()
            {
                AuctionId = auctionId
            };

            State.AuctionToProductDic[auctionId] = productId;


            return auctionId;
        }

        public override Empty Initialize(InitializeDto input)
        {
            State.AuctionContract.Value = input.AuctionContractAddress;

            return new Empty();
        }

        public override Empty __callback_auction(AuctionNotification input)
        {
            if (Context.Sender !=
                Context.ConvertVirtualAddressToContractAddress(input.AuctionId, State.AuctionContract.Value))
                throw new AssertionException("callback sender should be a virtual address of auction");


            var productId = State.AuctionToProductDic[input.AuctionId];
            
            State.Products[productId].Owner = input.Winner;

            Context.Fire(new CallbackEvent() {AuctionId = input.AuctionId});

            return new Empty();
        }
    }
}