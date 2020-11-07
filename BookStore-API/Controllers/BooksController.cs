using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore_API.Controllers
{
    /// <summary>
    /// Endpoint used to interact with the Books in the book store's database
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _IBookRepository;
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;

        public BooksController(IBookRepository IBookRepository, ILoggerService logger, IMapper mapper)
        {
            _IBookRepository = IBookRepository;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Get All Books
        /// </summary>
        /// <returns>List Of Authors</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBooks()
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location}: Attempted Get All Books");
                var authors = await _IBookRepository.FindAll();
                var response = _mapper.Map<IList<BookDTO>>(authors);
                _logger.LogInfo($"{location}: Succesfully got All Authors");
                return Ok(response);
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Get Book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Book by id</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBook(int id)
        {
            var location = GetControllerActionNames();
            try
            {
                var Book = await _IBookRepository.FindById(id);
                if (Book == null)
                {
                    _logger.LogWarn($"{location}: Book with id {id} was not found");
                    return NotFound();
                }
                var response = _mapper.Map<BookDTO>(Book);
                _logger.LogInfo($"{location}: Succesfuly got Book with id {id}");
                return Ok(response);
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Create an Book
        /// </summary>
        /// <param name="Book"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] BookCreateDTO Book)
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location}: Book Submission Attempted");
                if (Book == null)
                {
                    _logger.LogWarn($"{location}: Empty request was submitted");
                    return BadRequest(ModelState);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"{location}: Book Data was incomplete");
                    return BadRequest(ModelState);
                }
                var mappedBook = _mapper.Map<Book>(Book);
                var isSucces = await _IBookRepository.Create(mappedBook);

                if (!isSucces)
                {
                    return InternalError($"{location}: Book Creation failed");
                }
                _logger.LogInfo($"{location}: Book Created");
                return Created("Create", new { mappedBook });



            }
            catch (Exception e)
            {

                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Update an Book
        /// </summary>
        /// <param name="id"></param>
        /// <param name="BookDTO"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] BookUpdateDTO BookDTO)
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location}: update attempted - id:{id}");
                if (id < 1 || BookDTO == null || id != BookDTO.Id)
                {
                    _logger.LogWarn($"{location}: Author update failed with bad data");
                    return BadRequest();
                }
                var isExists = await _IBookRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn($"{location}: Author with id: {id} not found");
                    return NotFound();
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"{location}: Author update failed with incomplete data");
                    return BadRequest(ModelState);
                }
                var book = _mapper.Map<Book>(BookDTO);
                var isSucces = await _IBookRepository.Update(book);
                if (!isSucces)
                {
                    return InternalError($"{location}: Update operation failed");
                }
                _logger.LogInfo($"{location}: Author update succesfully");
                return NoContent();

            }
            catch (Exception e)
            {
                return InternalError($"{e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Delete Book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var location = GetControllerActionNames();
            try
            {
                if (id < 1)
                {
                    _logger.LogWarn($"{location}: delete failed with bad data");
                    return BadRequest();
                }
                var book = await _IBookRepository.FindById(id);
                if (book == null)
                {
                    _logger.LogWarn($"{location}: with id: {id} not found");
                    return NotFound();
                }
                var isSucces = await _IBookRepository.Delete(book);
                if (!isSucces)
                {
                    return InternalError($"{location}: Delete operation failed");
                }
                _logger.LogInfo($"{location}: Delete succesfully");
                return NoContent();

            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        private ObjectResult InternalError(string message)
        {
            _logger.LogError(message);
            return StatusCode(500, "Something went wrong. Please contact the Administrator");
        }

        private string GetControllerActionNames()
        {
            var controller = ControllerContext.ActionDescriptor.ControllerName;
            var action = ControllerContext.ActionDescriptor.ActionName;

            return $"{controller} - {action}";
        }
    }
}
