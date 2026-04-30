using SearchAgentService.Models;

namespace SearchAgentService.Repository;

public interface ISearchAgentRepository
{
    List<SearchAgent> GetAll();
    SearchAgent Create(SearchAgent agent);
    void DeleteById(int id);
    void DeleteByEmail(string email);
}