using AElf.Contracts.Auction;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
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

        public override Empty Create(Empty input)
        {
            var auctionId = Context.GenerateId(State.AuctionContract.Value, null);

            State.AuctionContract.Create.Send(new CreateDto()
            {
                Callback = Context.Self,
                Receiver = Context.Sender,
                ExpiredDate = Context.CurrentBlockTime.AddSeconds(100),
                MinAmount = 1,
                TokenSymbol = "ELF"
            });

            return new Empty();
        }

        public override Empty Initialize(InitializeDto input)
        {
            State.AuctionContract.Value = input.AuctionContractAddress;

            return new Empty();
        }
    }
}