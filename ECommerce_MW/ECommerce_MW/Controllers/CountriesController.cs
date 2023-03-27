﻿using ECommerce_MW.DAL;
using ECommerce_MW.DAL.Entities;
using ECommerce_MW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce_MW.Controllers
{
    public class CountriesController : Controller
    {
        #region Constructor

        private readonly DatabaseContext _context;

        public CountriesController(DatabaseContext context)
        {
            _context = context;
        }

        #endregion

        #region Private Methods
        private async Task<Country> GetCountryById(Guid? countryId)
        {
            Country country = await _context.Countries
                .Include(c => c.States)
                .FirstOrDefaultAsync(c => c.Id == countryId);

            return country;
        }

        #endregion

        #region Country Actions

        public async Task<IActionResult> Index()
        {
            return View(await _context.Countries
                .Include(c => c.States)
                .ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Country country)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    country.CreatedDate = DateTime.Now;
                    _context.Add(country);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbUpdateException)
                {
                    if (dbUpdateException.InnerException.Message.Contains("duplicate"))
                    {
                        ModelState.AddModelError(string.Empty, "Ya existe un país con el mismo nombre.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, dbUpdateException.InnerException.Message);
                    }
                }
                catch (Exception exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                }
            }
            return View(country);
        }

        public async Task<IActionResult> Edit(Guid? countryId)
        {
            if (countryId == null || _context.Countries == null)
            {
                return NotFound();
            }

            var country = await GetCountryById(countryId);
            if (country == null)
            {
                return NotFound();
            }
            return View(country);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Country country)
        {
            if (id != country.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    country.ModifiedDate = DateTime.Now;
                    _context.Update(country);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbUpdateException)
                {
                    if (dbUpdateException.InnerException.Message.Contains("duplicate"))
                    {
                        ModelState.AddModelError(string.Empty, "Ya existe un país con el mismo nombre.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, dbUpdateException.InnerException.Message);
                    }
                }
                catch (Exception exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                }
            }
            return View(country);
        }

        public async Task<IActionResult> Details(Guid? countryId)
        {
            if (countryId == null || _context.Countries == null)
            {
                return NotFound();
            }

            var country = await _context.Countries
                .Include(c => c.States)
                .FirstOrDefaultAsync(m => m.Id == countryId);
            if (country == null)
            {
                return NotFound();
            }

            return View(country);
        }

        public async Task<IActionResult> Delete(Guid? countryId)
        {
            if (countryId == null || _context.Countries == null)
            {
                return NotFound();
            }

            Country country = await _context.Countries
                .Include(c => c.States)
                .FirstOrDefaultAsync(c => c.Id == countryId);
            if (country == null)
            {
                return NotFound();
            }

            return View(country);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Countries == null)
            {
                return Problem("Entity set 'DatabaseContext.Countries' is null.");
            }
            var country = await _context.Countries
                .Include(c => c.States)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country != null)
            {
                _context.Countries.Remove(country);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region State Actions

        [HttpGet]
        public async Task<IActionResult> AddState(Guid? countryId)
        {
            if (countryId == null) return NotFound();

            Country country = await GetCountryById(countryId);

            if (country == null) return NotFound();

            StateViewModel stateViewModel = new()
            {
                CountryId = country.Id,
            };

            return View(stateViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddState(StateViewModel stateViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    State state = new State()
                    {
                        Cities = new List<City>(),
                        Country = await GetCountryById(stateViewModel.CountryId),
                        Name = stateViewModel.Name,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = null,
                    };

                    _context.Add(state);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { stateViewModel.CountryId });
                }
                catch (DbUpdateException dbUpdateException)
                {
                    if (dbUpdateException.InnerException.Message.Contains("duplicate"))
                    {
                        ModelState.AddModelError(string.Empty, "Ya existe un Dpto/Estado con el mismo nombre en este país.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, dbUpdateException.InnerException.Message);
                    }
                }
                catch (Exception exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                }
            }
            return View(stateViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditState(Guid? stateId)
        {
            if (stateId == null || _context.States == null)
            {
                return NotFound();
            }

            State state = await _context.States
                .Include(s => s.Country)
                .FirstOrDefaultAsync(s => s.Id == stateId);

            if (state == null)
            {
                return NotFound();
            }

            StateViewModel stateViewModel = new()
            {
                CountryId = state.Country.Id,
                Id = state.Id,
                Name = state.Name,
                CreatedDate = state.CreatedDate,
            };

            return View(stateViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditState(Guid countryId, StateViewModel stateViewModel)
        {
            if (countryId != stateViewModel.CountryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    State state = new()
                    {
                        Id = stateViewModel.Id,
                        Name = stateViewModel.Name,
                        CreatedDate = stateViewModel.CreatedDate,
                        ModifiedDate = DateTime.UtcNow,
                    };

                    _context.Update(state);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { stateViewModel.CountryId });
                }
                catch (DbUpdateException dbUpdateException)
                {
                    if (dbUpdateException.InnerException.Message.Contains("duplicate"))
                    {
                        ModelState.AddModelError(string.Empty, "Ya existe un estado con el mismo nombre.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, dbUpdateException.InnerException.Message);
                    }
                }
                catch (Exception exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                }
            }
            return View(stateViewModel);
        }

        public async Task<IActionResult> DetailsState(Guid? stateId)
        {
            if (stateId == null || _context.States == null) return NotFound();

            State state = await _context.States
                .Include(s => s.Country)
                .Include(c => c.Cities)
                .FirstOrDefaultAsync(m => m.Id == stateId);

            if (state == null) return NotFound();

            return View(state);
        }

        public async Task<IActionResult> DeleteState(Guid? stateId)
        {
            if (stateId == null || _context.States == null) return NotFound();

            State state = await _context.States
                .Include(s => s.Country)
                .Include(c => c.Cities)
                .FirstOrDefaultAsync(c => c.Id == stateId);

            if (state == null) return NotFound();

            return View(state);
        }

        [HttpPost, ActionName("DeleteState")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStateConfirmed(Guid stateId)
        {
            if (_context.States == null) return Problem("Entity set 'DatabaseContext.States' is null.");

            State state = await _context.States
                .Include(s => s.Country)
                .Include(c => c.Cities)
                .FirstOrDefaultAsync(c => c.Id == stateId);

            if (state != null) _context.States.Remove(state);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { countryId = state.Country.Id });
        }

        #endregion

        #region City Actions



        #endregion
    }
}
