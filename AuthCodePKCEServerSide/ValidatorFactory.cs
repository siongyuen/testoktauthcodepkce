namespace AuthCodePKCEServerSide
{
    public interface IValidatorFactory
      {
        Task<ICustomTokenHelper> GetTokenHelper(string issuer);
    }

    public class ValidatorFactory : IValidatorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidatorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<ICustomTokenHelper> GetTokenHelper(string issuer)
        {
            if (issuer == null)
            {
                throw new ArgumentNullException(nameof(issuer), "Issuer cannot be null.");
            }

            if (issuer.Contains("https://sts.windows.net"))
            {
                return Task.FromResult<ICustomTokenHelper>(new MicrosoftTokenValidator());
            }
            else
            {
                return Task.FromResult<ICustomTokenHelper>(new CustomTokenValidator());
            }
        }
    }

}
