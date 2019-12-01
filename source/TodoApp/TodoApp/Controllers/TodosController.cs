using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using TodoModel;

namespace TodoApp.Controllers
{
    public class TodosController : Controller
    {
        TodoApiClient apiClient;

        public TodosController(IHttpClientFactory httpClientFactory)
        {
            HttpClient client = httpClientFactory.CreateClient(Constants.HttpClientName);
            apiClient = new TodoApiClient(client);
        }

        // GET: Todos
        public async Task<ActionResult> Index()
        {            
            Trace.WriteLine("GET /Todos/Index");

            List<TodoItem> todoItems = await apiClient.SendAsync<List<TodoItem>>(HttpMethod.Get);
            return View(todoItems);
        }

        // GET: Todos/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            Trace.WriteLine("GET /Todos/Details/" + id);
            if (id == null)
            {
                return BadRequest();
            }

            TodoItem todo = await apiClient.SendAsync<TodoItem>(HttpMethod.Get, id.ToString());
            if (todo == null)
            {
                return NotFound();
            }
            return View(todo);
        }

        // GET: Todos/Create
        public ActionResult Create()
        {
            Trace.WriteLine("GET /Todos/Create");
            return View(new TodoItem());
        }

        // POST: Todos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Id, Name, IsComplete")]TodoItem todo)
        {
            Trace.WriteLine("POST /Todos/Create");
            if (ModelState.IsValid)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "TodoItems");
                request.Content = new StringContent(JsonConvert.SerializeObject(todo), Encoding.UTF8, "application/json");
                todo = await apiClient.SendAsync<TodoItem>(request);
                return RedirectToAction("Index");
            }

            return View(todo);
        }

        // GET: Todos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            Trace.WriteLine("GET /Todos/Edit/" + id);
            if (id == null)
            {
                return new BadRequestResult();
            }

            TodoItem todo = await apiClient.SendAsync<TodoItem>(HttpMethod.Get, id.ToString());
            if (todo == null)
            {
                return NotFound();
            }
            return View(todo);
        }

        // POST: Todos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind("Id,Name,IsComplete")]TodoItem todo)
        {
            Trace.WriteLine("POST /Todos/Edit/" + todo.Id);
            if (ModelState.IsValid)
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{TodoApiClient.TodoApiUrl}/{todo.Id.ToString()}");
                request.Content = new StringContent(JsonConvert.SerializeObject(todo), Encoding.UTF8, "application/json");
                await apiClient.SendAsync(request);
                return RedirectToAction("Index");
            }
            return View(todo);
        }

        // GET: Todos/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            Trace.WriteLine("GET /Todos/Delete/" + id);
            if (id == null)
            {
                return new BadRequestResult();
            }
            TodoItem todo = await apiClient.SendAsync<TodoItem>(HttpMethod.Get, id.ToString());
            if (todo == null)
            {
                return NotFound();
            }
            return View(todo);
        }

        // POST: Todos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Trace.WriteLine("POST /Todos/Delete/" + id);
            TodoItem todo = await apiClient.SendAsync<TodoItem>(HttpMethod.Delete, id.ToString());
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class TodoApiClient
    {
        public static readonly string TodoApiUrl = "TodoItems";
        private readonly HttpClient client;
        private static readonly List<HttpStatusCode> successCodes = new List<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent };
        public TodoApiClient(HttpClient client)
        {
            this.client = client;
        }

        public async Task<int> SendAsync(HttpRequestMessage request)
        {
            var response = await client.SendAsync(request);
            HttpStatusCode? status = (HttpStatusCode?)successCodes.Find(x => x == response.StatusCode);
            if (response.IsSuccessStatusCode && status.HasValue)
            {
                return await Task.FromResult(0);
            }
            else
            {
                throw new Exception($"Something bad happened.\n ResponseCode: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            }
        }
        public async Task<T> SendAsync<T>(HttpMethod method, string requestUrl)
        {
            var request = new HttpRequestMessage(method, $"{TodoApiUrl}/{requestUrl}");
            return await SendAsync<T>(request);
        }

        public async Task<T> SendAsync<T>(HttpMethod method)
        {
            var request = new HttpRequestMessage(method, TodoApiUrl);
            return await SendAsync<T>(request);
        }

        public async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            var response = await client.SendAsync(request);
            HttpStatusCode? status = (HttpStatusCode?)successCodes.Find(x => x == response.StatusCode);
            if (response.IsSuccessStatusCode && status.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default(T);
                }
                else
                {
                    throw new Exception($"Something bad happened.\n ResponseCode: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
        }
    }
}
