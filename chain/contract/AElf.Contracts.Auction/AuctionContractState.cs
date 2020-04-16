using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Auction
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public class AuctionContractState : ContractState
    {
        // state definitions go here.

        public MappedState<Hash,Auction> Auctions { get; set; }
        
    }
}