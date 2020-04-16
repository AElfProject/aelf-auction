using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Auction
{
    /// <summary>
    /// The C# implementation of the contract defined in auction_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public class AuctionContract : AuctionContractContainer.AuctionContractBase
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

        public override CreateResultDto Create(CreateDto input)
        {
            var id = Context.TransactionId;

            Assert(State.Auctions[id] == null, "auction already exists");

            Auction auction = new Auction()
            {
                Address = input.Address,
                Status = AuctionStatus.Active,
                ExpiredDate = input.ExpiredDate,
                MinAmount = input.MinAmount,
                TokenSymbol = input.TokenSymbol,
            };
            State.Auctions[id] = auction;

            return new CreateResultDto()
            {
                Id = id
            };
        }

        public override BidResultDto Bid(BidDto input)
        {
            return new BidResultDto()
            {
                Status = BidStatus.Awarded
            };
        }

        public override Empty Initialize(InitializeDto input)
        {
            
            return new Empty();
        }
    }
}