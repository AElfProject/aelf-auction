using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
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
                Status = AuctionStatus.Active,
                ExpiredDate = input.ExpiredDate,
                MinAmount = input.MinAmount,
                TokenSymbol = input.TokenSymbol,
                Callback = input.Callback,
                Receiver = input.Receiver,
            };
            State.Auctions[id] = auction;

            return new CreateResultDto()
            {
                Id = id
            };
        }

        public override Empty ReleaseAuction(Hash id)
        {
            var auction = State.Auctions[id];
            Assert(auction != null, "auction not exists");

            ReleaseAuction(id, auction);

            return new Empty();
        }

        public void ReleaseAuction(Hash id, Auction auction)
        {
            if (Context.CurrentBlockTime >= auction.ExpiredDate)
            {
                if (auction.LastBidderAmount > 0)
                {
                    //give the money to receiver
                    Context.SendVirtualInline(
                        id,
                        State.TokenContract.Value,
                        nameof(State.TokenContract.Transfer),
                        new TransferInput()
                        {
                            Amount = auction.LastBidderAmount,
                            Symbol = auction.TokenSymbol,
                            To = auction.Receiver,
                        });

                    if (!auction.Callback.Value.IsEmpty)
                    {
                        Context.SendInline(auction.Callback, "__callback_auction",
                            new AuctionNotification()
                            {
                                Winner = auction.LastBidder,
                                AuctionId = id
                            });
                    }

                    Context.Fire(new BidSuccessEvent()
                    {
                        Amount = auction.LastBidderAmount,
                        Bidder = auction.LastBidder,
                        Id = id,
                        Symbol = auction.TokenSymbol
                    });
                }

                State.Auctions.Remove(id);
            }
        }

        public override BidResultDto Bid(BidDto input)
        {
            var auction = State.Auctions[input.Id];
            Assert(auction != null, "auction not exists");
            if (auction.ExpiredDate <= Context.CurrentBlockTime)
            {
                ReleaseAuction(input.Id, auction);
                return new BidResultDto()
                {
                    Status = BidStatus.Rejected
                };
            }

            if (auction.LastBidderAmount > input.Amount || auction.MinAmount > input.Amount)
            {
                return new BidResultDto()
                {
                    Status = BidStatus.Rejected
                };
            }


            var vAddressToken = GetSenderVirtualAddressToken();


            if (auction.LastBidderAmount > 0)
            {
                //give the money back to the last bidder 
                Context.SendVirtualInline(
                    input.Id,
                    State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer),
                    new TransferInput()
                    {
                        Amount = auction.LastBidderAmount,
                        Symbol = auction.TokenSymbol,
                        To = GetVirtualAddress(auction.LastBidder),
                    });
            }

            //take the money of current bidder 
            Context.SendVirtualInline(
                vAddressToken,
                State.TokenContract.Value,
                nameof(State.TokenContract.Transfer),
                new TransferInput()
                {
                    Amount = input.Amount,
                    Symbol = auction.TokenSymbol,
                    To = Context.ConvertVirtualAddressToContractAddress(input.Id),
                });

            auction.LastBidder = Context.Sender;
            auction.LastBidderAmount = input.Amount;

            Context.Fire(new BidEvent()
            {
                Id = input.Id,
                Amount = auction.LastBidderAmount,
                Bidder = auction.LastBidder,
                Symbol = auction.TokenSymbol
            });

            return new BidResultDto()
            {
                Status = BidStatus.Awarded
            };
        }

        public override Empty Initialize(InitializeDto input)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }

        public override Address GetVirtualAddress(Address input)
        {
            return Context.ConvertVirtualAddressToContractAddress(GetVirtualAddressToken(input));
        }

        private Hash GetVirtualAddressToken(Address address)
        {
            return Hash.FromRawBytes(address.ToByteArray());
        }

        private Hash GetSenderVirtualAddressToken()
        {
            return GetVirtualAddressToken(Context.Sender);
        }

        public override Address GetSenderVirtualAddress(Empty input)
        {
            return GetSenderVirtualAddress();
        }

        private Address GetSenderVirtualAddress()
        {
            return GetVirtualAddress(Context.Sender);
        }
    }
}