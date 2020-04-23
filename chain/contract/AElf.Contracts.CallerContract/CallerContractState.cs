using AElf.Contracts.Auction;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.CallerContract
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public class CallerContractState : ContractState
    {
        // state definitions go here.

        internal AuctionContractContainer.AuctionContractReferenceState AuctionContract { get; set; }

        public MappedState<Hash, Hash> AuctionToProductDic { get; set; }

        public MappedState<Hash, Product> Products { get; set; }
    }
}