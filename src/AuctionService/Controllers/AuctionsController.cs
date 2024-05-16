using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/auctions")]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDBContext _context;
        private readonly IMapper _mapper;
        public AuctionsController(AuctionDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
        {
            var allAuctions = await _context.Auctions
            .Include(x => x.Item)
            .OrderBy(x => x.Item.Make)
            .ToListAsync();

            return _mapper.Map<List<AuctionDto>>(allAuctions);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await _context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            return _mapper.Map<AuctionDto>(auction);
        }
        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            var newAuction = _mapper.Map<Auction>(auctionDto);

            _context.Auctions.Add(newAuction);

            bool savedToDb = await _context.SaveChangesAsync() > 0;

            if (!savedToDb) return BadRequest("Could not save new auction to DB");

            return CreatedAtAction(nameof(GetAuctionById), new {newAuction.Id}, _mapper.Map<AuctionDto>(newAuction));
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            var existingAuction = await _context.Auctions.Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existingAuction == null) 
                return NotFound();

            if (existingAuction.CurrentHighBid > 0)
                throw new ApplicationException("Cannot update auction that already has bids");

            existingAuction.Item.Make = updateAuctionDto.Make ?? existingAuction.Item.Make;
            existingAuction.Item.Model = updateAuctionDto.Model ?? existingAuction.Item.Model;
            existingAuction.Item.Color = updateAuctionDto.Color ?? existingAuction.Item.Color;
            existingAuction.Item.Mileage = updateAuctionDto.Mileage ?? existingAuction.Item.Mileage;
            existingAuction.Item.Year = updateAuctionDto.Year ?? existingAuction.Item.Year;

            bool updatedOnDb = await _context.SaveChangesAsync() > 0;

            if (updatedOnDb) 
                return Ok();
            else
                return BadRequest("Problem updating auction item on DB");
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var existingAuction = await _context.Auctions.FindAsync(id);

            if (existingAuction == null) return NotFound();

            // TODO: Check seller == username

            _context.Auctions.Remove(existingAuction);

            bool deleted = await _context.SaveChangesAsync() > 0;

            if (!deleted)
                return BadRequest("Could not delete the auction");
            else
                return Ok();
        }
    }
}